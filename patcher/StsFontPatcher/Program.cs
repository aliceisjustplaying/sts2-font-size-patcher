using Mono.Cecil;
using Mono.Cecil.Cil;

if (args.Length < 1) { Console.WriteLine("Usage: dotnet run -- <path-to-sts2.dll> [scale-factor]"); return 1; }
var dllPath = args[0];
var scaleFactor = args.Length >= 2 ? double.Parse(args[1]) : 1.75;
if (!File.Exists(dllPath)) { Console.WriteLine($"ERROR: File not found: {dllPath}"); return 1; }

var backupPath = dllPath + ".bak";
if (!File.Exists(backupPath)) { File.Copy(dllPath, backupPath); Console.WriteLine($"Backed up to: {backupPath}"); }

Console.WriteLine($"Scale factor: {scaleFactor:F2}x");

var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(dllPath))!);
resolver.AddSearchDirectory(AppContext.BaseDirectory);

var godotSharpPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(dllPath))!, "GodotSharp.dll");
if (!File.Exists(godotSharpPath)) { Console.WriteLine($"ERROR: GodotSharp not found next to sts2.dll: {godotSharpPath}"); return 1; }
var godotSharpBackupPath = godotSharpPath + ".bak";
if (!File.Exists(godotSharpBackupPath)) { File.Copy(godotSharpPath, godotSharpBackupPath); Console.WriteLine($"Backed up to: {godotSharpBackupPath}"); }

var readerParameters = new ReaderParameters { AssemblyResolver = resolver, InMemory = true };
using var assembly = AssemblyDefinition.ReadAssembly(dllPath, readerParameters);
using var godotSharpTarget = AssemblyDefinition.ReadAssembly(godotSharpPath, readerParameters);

var module = assembly.MainModule;
var godotSharp = godotSharpTarget;

TypeDefinition GType(string n) => godotSharp.MainModule.Types.First(t => t.FullName == n);
var controlType = GType("Godot.Control");
var nodeType = GType("Godot.Node");
var labelType = GType("Godot.Label");
var rtlType = GType("Godot.RichTextLabel");
var resourceType = GType("Godot.Resource");
var labelSettingsType = GType("Godot.LabelSettings");
var sceneTreeType = GType("Godot.SceneTree");
var stringNameType = GType("Godot.StringName");

var getThemeFontSize = module.ImportReference(controlType.Methods.First(m => m.Name == "GetThemeFontSize"));
var addThemeFontSizeOverride = module.ImportReference(controlType.Methods.First(m => m.Name == "AddThemeFontSizeOverride"));
var getChildCount = module.ImportReference(nodeType.Methods.First(m => m.Name == "GetChildCount" && m.Parameters.Count == 1));
var getChild = module.ImportReference(nodeType.Methods.First(m =>
    m.Name == "GetChild" &&
    m.Parameters.Count == 2 &&
    !m.HasGenericParameters &&
    m.ReturnType.FullName == "Godot.Node"));
var snImplicit = module.ImportReference(stringNameType.Methods.First(m => m.Name == "op_Implicit" && m.Parameters[0].ParameterType.FullName == "System.String"));
var getTree = module.ImportReference(nodeType.Methods.First(m => m.Name == "GetTree" && m.Parameters.Count == 0 && m.ReturnType.Name == "SceneTree"));
var duplicateResource = module.ImportReference(resourceType.Methods.First(m => m.Name == "Duplicate" && m.Parameters.Count == 1));
var getLabelSettings = module.ImportReference(labelType.Methods.First(m => m.Name == "GetLabelSettings" && m.Parameters.Count == 0));
var setLabelSettings = module.ImportReference(labelType.Methods.First(m => m.Name == "SetLabelSettings" && m.Parameters.Count == 1));
var getLabelSettingsFontSize = module.ImportReference(labelSettingsType.Methods.First(m => m.Name == "GetFontSize" && m.Parameters.Count == 0));
var setLabelSettingsFontSize = module.ImportReference(labelSettingsType.Methods.First(m => m.Name == "SetFontSize" && m.Parameters.Count == 1));
var getLabelSettingsOutlineSize = module.ImportReference(labelSettingsType.Methods.First(m => m.Name == "GetOutlineSize" && m.Parameters.Count == 0));
var setLabelSettingsOutlineSize = module.ImportReference(labelSettingsType.Methods.First(m => m.Name == "SetOutlineSize" && m.Parameters.Count == 1));
var getLabelSettingsShadowSize = module.ImportReference(labelSettingsType.Methods.First(m => m.Name == "GetShadowSize" && m.Parameters.Count == 0));
var setLabelSettingsShadowSize = module.ImportReference(labelSettingsType.Methods.First(m => m.Name == "SetShadowSize" && m.Parameters.Count == 1));
var stringConcat2 = module.ImportReference(typeof(string).GetMethod(nameof(string.Concat), new[]
{
    typeof(string),
    typeof(string)
})!);
var stringConcat4 = module.ImportReference(typeof(string).GetMethod(nameof(string.Concat), new[]
{
    typeof(string),
    typeof(string),
    typeof(string),
    typeof(string)
})!);

int patchCount = 0;
int godotPatchCount = 0;

// PATCH 1: MegaLabel/MegaRichTextLabel.SetFontSize — multiply size param
foreach (var cls in new[] { "MegaCrit.Sts2.addons.mega_text.MegaLabel", "MegaCrit.Sts2.addons.mega_text.MegaRichTextLabel" })
{
    var m = module.Types.FirstOrDefault(t => t.FullName == cls)?.Methods.FirstOrDefault(m => m.Name == "SetFontSize");
    if (m == null) continue;
    var il = m.Body.GetILProcessor();
    var first = m.Body.Instructions[0];
    il.InsertBefore(first, il.Create(OpCodes.Ldarg_1));
    il.InsertBefore(first, il.Create(OpCodes.Conv_R8));
    il.InsertBefore(first, il.Create(OpCodes.Ldc_R8, scaleFactor));
    il.InsertBefore(first, il.Create(OpCodes.Mul));
    il.InsertBefore(first, il.Create(OpCodes.Conv_I4));
    il.InsertBefore(first, il.Create(OpCodes.Starg_S, m.Parameters[0]));
    Console.WriteLine($"PATCH 1: {cls}.SetFontSize"); patchCount++;
}

// PATCH 1B: MegaLabel/MegaRichTextLabel._Ready — apply scaled base theme overrides generically
{
    void InsertScaledThemeOverride(MethodDefinition method, Instruction target, string theme, params string[] props)
    {
        var il = method.Body.GetILProcessor();
        foreach (var prop in props)
        {
            il.InsertBefore(target, il.Create(OpCodes.Ldarg_0));
            il.InsertBefore(target, il.Create(OpCodes.Ldstr, prop));
            il.InsertBefore(target, il.Create(OpCodes.Call, snImplicit));
            il.InsertBefore(target, il.Create(OpCodes.Ldarg_0));
            il.InsertBefore(target, il.Create(OpCodes.Ldstr, prop));
            il.InsertBefore(target, il.Create(OpCodes.Call, snImplicit));
            il.InsertBefore(target, il.Create(OpCodes.Ldstr, theme));
            il.InsertBefore(target, il.Create(OpCodes.Call, snImplicit));
            il.InsertBefore(target, il.Create(OpCodes.Callvirt, getThemeFontSize));
            il.InsertBefore(target, il.Create(OpCodes.Conv_R8));
            il.InsertBefore(target, il.Create(OpCodes.Ldc_R8, scaleFactor));
            il.InsertBefore(target, il.Create(OpCodes.Mul));
            il.InsertBefore(target, il.Create(OpCodes.Conv_I4));
            il.InsertBefore(target, il.Create(OpCodes.Callvirt, addThemeFontSizeOverride));
        }
    }

    var megaLabelType = module.Types.FirstOrDefault(t => t.FullName == "MegaCrit.Sts2.addons.mega_text.MegaLabel");
    var megaRtlType = module.Types.FirstOrDefault(t => t.FullName == "MegaCrit.Sts2.addons.mega_text.MegaRichTextLabel");

    var megaLabelReady = megaLabelType?.Methods.FirstOrDefault(m => m.Name == "_Ready");
    if (megaLabelReady != null)
    {
        var target = megaLabelReady.Body.Instructions.FirstOrDefault(i =>
            i.OpCode == OpCodes.Call &&
            i.Operand is MethodReference mr &&
            mr.Name == "AdjustFontSize");

        if (target != null)
        {
            InsertScaledThemeOverride(megaLabelReady, target, "Label", "font_size");
            Console.WriteLine("PATCH 1B: MegaLabel._Ready base font override");
            patchCount++;
        }
    }

    var megaRtlReady = megaRtlType?.Methods.FirstOrDefault(m => m.Name == "_Ready");
    if (megaRtlReady != null)
    {
        var target = megaRtlReady.Body.Instructions.FirstOrDefault(i =>
            i.OpCode == OpCodes.Call &&
            i.Operand is MethodReference mr &&
            mr.Name == "AdjustFontSize");

        if (target != null)
        {
            InsertScaledThemeOverride(megaRtlReady, target, "RichTextLabel",
                "normal_font_size",
                "bold_font_size",
                "italics_font_size",
                "bold_italics_font_size",
                "mono_font_size");
            Console.WriteLine("PATCH 1B: MegaRichTextLabel._Ready base font overrides");
            patchCount++;
        }
    }
}

// PATCH 1C: Debug footer — show font patch version tag and use MegaLabel auto-sizing
{
    var megaLabelType = module.Types.FirstOrDefault(t => t.FullName == "MegaCrit.Sts2.addons.mega_text.MegaLabel");
    var debugInfoType = module.Types.FirstOrDefault(t => t.FullName == "MegaCrit.Sts2.Core.Nodes.Debug.NDebugInfoLabelManager");
    var setTextAutoSize = megaLabelType == null
        ? null
        : module.ImportReference(megaLabelType.Methods.First(m =>
            m.Name == "SetTextAutoSize" &&
            m.Parameters.Count == 1 &&
            m.Parameters[0].ParameterType.FullName == "System.String"));

    if (debugInfoType != null && setTextAutoSize != null)
    {
        var formatFooter = debugInfoType.Methods.FirstOrDefault(m => m.Name == "_FormatFontPatchFooter" && m.Parameters.Count == 2);
        if (formatFooter == null)
        {
            formatFooter = new MethodDefinition("_FormatFontPatchFooter",
                Mono.Cecil.MethodAttributes.Private | Mono.Cecil.MethodAttributes.Static | Mono.Cecil.MethodAttributes.HideBySig,
                module.TypeSystem.String);
            formatFooter.Parameters.Add(new ParameterDefinition("version", Mono.Cecil.ParameterAttributes.None, module.TypeSystem.String));
            formatFooter.Parameters.Add(new ParameterDefinition("date", Mono.Cecil.ParameterAttributes.None, module.TypeSystem.String));

            var il = formatFooter.Body.GetILProcessor();
            il.Append(il.Create(OpCodes.Ldstr, "["));
            il.Append(il.Create(OpCodes.Ldarg_0));
            il.Append(il.Create(OpCodes.Ldstr, $" + Font Patch {scaleFactor:F2}x] ["));
            il.Append(il.Create(OpCodes.Ldarg_1));
            il.Append(il.Create(OpCodes.Call, stringConcat4));
            il.Append(il.Create(OpCodes.Ldstr, "]"));
            il.Append(il.Create(OpCodes.Call, stringConcat2));
            il.Append(il.Create(OpCodes.Ret));

            debugInfoType.Methods.Add(formatFooter);
        }

        var updateText = debugInfoType.Methods.FirstOrDefault(m => m.Name == "UpdateText" && m.Parameters.Count == 1 && m.HasBody);
        if (updateText != null)
        {
            var releaseInfoField = debugInfoType.Fields.FirstOrDefault(f => f.Name == "_releaseInfo");
            var moddedWarningField = debugInfoType.Fields.FirstOrDefault(f => f.Name == "_moddedWarning");

            var renderStart = updateText.Body.Instructions.FirstOrDefault(i =>
                i.OpCode == OpCodes.Ldarg_0 &&
                i.Next?.OpCode == OpCodes.Ldfld &&
                i.Next.Operand is FieldReference fr &&
                fr.Name == "isMainMenu");

            var continueAt = updateText.Body.Instructions.FirstOrDefault(i =>
                i.OpCode == OpCodes.Ldarg_0 &&
                i.Next?.OpCode == OpCodes.Ldfld &&
                i.Next.Operand is FieldReference fr &&
                fr.Name == "_moddedWarning");

            if (releaseInfoField != null && moddedWarningField != null && renderStart != null && continueAt != null)
            {
                var il = updateText.Body.GetILProcessor();
                il.InsertBefore(renderStart, il.Create(OpCodes.Ldarg_0));
                il.InsertBefore(renderStart, il.Create(OpCodes.Ldfld, releaseInfoField));
                il.InsertBefore(renderStart, il.Create(OpCodes.Ldloc_2));
                il.InsertBefore(renderStart, il.Create(OpCodes.Ldloc_1));
                il.InsertBefore(renderStart, il.Create(OpCodes.Call, formatFooter));
                il.InsertBefore(renderStart, il.Create(OpCodes.Callvirt, setTextAutoSize));
                il.InsertBefore(renderStart, il.Create(OpCodes.Br, continueAt));

                Console.WriteLine("PATCH 1C: NDebugInfoLabelManager.UpdateText footer text");
                patchCount++;
            }
        }
    }
}

// PATCH 2: NGame._Ready — hook SceneTree.NodeAdded and scale the existing tree
{
    var nGame = module.Types.FirstOrDefault(t => t.FullName == "MegaCrit.Sts2.Core.Nodes.NGame");
    var nReady = nGame?.Methods.FirstOrDefault(m => m.Name == "_Ready");
    if (nGame != null && nReady != null)
    {
        var megaLabel = module.Types.First(t => t.FullName == "MegaCrit.Sts2.addons.mega_text.MegaLabel");
        var megaRtl = module.Types.First(t => t.FullName == "MegaCrit.Sts2.addons.mega_text.MegaRichTextLabel");
        var rtlTypeRef = module.ImportReference(rtlType);
        var labelTypeRef = module.ImportReference(labelType);
        var labelSettingsTypeRef = module.ImportReference(labelSettingsType);

        // Create static helper: _FontScaleOnNodeAdded(Node node)
        var helper = new MethodDefinition("_FontScaleOnNodeAdded",
            Mono.Cecil.MethodAttributes.Private | Mono.Cecil.MethodAttributes.Static | Mono.Cecil.MethodAttributes.HideBySig,
            module.ImportReference(typeof(void)));
        helper.Parameters.Add(new ParameterDefinition("node", Mono.Cecil.ParameterAttributes.None, module.ImportReference(nodeType)));
        helper.Body.InitLocals = true;
        helper.Body.Variables.Add(new VariableDefinition(rtlTypeRef));            // loc0: RichTextLabel
        helper.Body.Variables.Add(new VariableDefinition(labelTypeRef));          // loc1: Label
        helper.Body.Variables.Add(new VariableDefinition(labelSettingsTypeRef));  // loc2: LabelSettings

        var h = helper.Body.GetILProcessor();

        void EmitScaleIntFromStack(ILProcessor il)
        {
            il.Append(il.Create(OpCodes.Conv_R8));
            il.Append(il.Create(OpCodes.Ldc_R8, scaleFactor));
            il.Append(il.Create(OpCodes.Mul));
            il.Append(il.Create(OpCodes.Conv_I4));
        }

        // Emit helper to add one font size override using a local.
        void AddOverride(int loc, string prop, string theme)
        {
            var ldloc = loc == 0 ? OpCodes.Ldloc_0 : OpCodes.Ldloc_1;
            h.Append(h.Create(ldloc));
            h.Append(h.Create(OpCodes.Ldstr, prop));
            h.Append(h.Create(OpCodes.Call, snImplicit));
            h.Append(h.Create(ldloc));
            h.Append(h.Create(OpCodes.Ldstr, prop));
            h.Append(h.Create(OpCodes.Call, snImplicit));
            h.Append(h.Create(OpCodes.Ldstr, theme));
            h.Append(h.Create(OpCodes.Call, snImplicit));
            h.Append(h.Create(OpCodes.Callvirt, getThemeFontSize));
            EmitScaleIntFromStack(h);
            h.Append(h.Create(OpCodes.Callvirt, addThemeFontSizeOverride));
        }

        // if (node is MegaLabel) return;
        var ck1 = h.Create(OpCodes.Nop);
        h.Append(h.Create(OpCodes.Ldarg_0));
        h.Append(h.Create(OpCodes.Isinst, megaLabel));
        h.Append(h.Create(OpCodes.Brfalse, ck1));
        h.Append(h.Create(OpCodes.Ret));
        h.Append(ck1);

        // if (node is MegaRichTextLabel) return;
        var ck2 = h.Create(OpCodes.Nop);
        h.Append(h.Create(OpCodes.Ldarg_0));
        h.Append(h.Create(OpCodes.Isinst, megaRtl));
        h.Append(h.Create(OpCodes.Brfalse, ck2));
        h.Append(h.Create(OpCodes.Ret));
        h.Append(ck2);

        // RichTextLabel rtl = node as RichTextLabel;
        h.Append(h.Create(OpCodes.Ldarg_0));
        h.Append(h.Create(OpCodes.Isinst, rtlTypeRef));
        h.Append(h.Create(OpCodes.Stloc_0));

        // if (rtl != null) { ... return; }
        var ck3 = h.Create(OpCodes.Nop);
        h.Append(h.Create(OpCodes.Ldloc_0));
        h.Append(h.Create(OpCodes.Brfalse, ck3));
        AddOverride(0, "normal_font_size", "RichTextLabel");
        AddOverride(0, "bold_font_size", "RichTextLabel");
        AddOverride(0, "italics_font_size", "RichTextLabel");
        AddOverride(0, "bold_italics_font_size", "RichTextLabel");
        AddOverride(0, "mono_font_size", "RichTextLabel");
        h.Append(h.Create(OpCodes.Ret));
        h.Append(ck3);

        // Label lbl = node as Label;
        h.Append(h.Create(OpCodes.Ldarg_0));
        h.Append(h.Create(OpCodes.Isinst, labelTypeRef));
        h.Append(h.Create(OpCodes.Stloc_1));

        // if (lbl != null) { ... return; }
        var ck4 = h.Create(OpCodes.Nop);
        h.Append(h.Create(OpCodes.Ldloc_1));
        h.Append(h.Create(OpCodes.Brfalse, ck4));

        // var settings = lbl.GetLabelSettings();
        h.Append(h.Create(OpCodes.Ldloc_1));
        h.Append(h.Create(OpCodes.Callvirt, getLabelSettings));
        h.Append(h.Create(OpCodes.Stloc_2));

        var afterSettings = h.Create(OpCodes.Nop);
        h.Append(h.Create(OpCodes.Ldloc_2));
        h.Append(h.Create(OpCodes.Brfalse, afterSettings));

        // settings = (LabelSettings)settings.Duplicate(false);
        h.Append(h.Create(OpCodes.Ldloc_2));
        h.Append(h.Create(OpCodes.Ldc_I4_0));
        h.Append(h.Create(OpCodes.Callvirt, duplicateResource));
        h.Append(h.Create(OpCodes.Castclass, labelSettingsTypeRef));
        h.Append(h.Create(OpCodes.Stloc_2));

        // settings.FontSize = Scale(settings.FontSize)
        h.Append(h.Create(OpCodes.Ldloc_2));
        h.Append(h.Create(OpCodes.Ldloc_2));
        h.Append(h.Create(OpCodes.Callvirt, getLabelSettingsFontSize));
        EmitScaleIntFromStack(h);
        h.Append(h.Create(OpCodes.Callvirt, setLabelSettingsFontSize));

        // settings.OutlineSize = Scale(settings.OutlineSize)
        h.Append(h.Create(OpCodes.Ldloc_2));
        h.Append(h.Create(OpCodes.Ldloc_2));
        h.Append(h.Create(OpCodes.Callvirt, getLabelSettingsOutlineSize));
        EmitScaleIntFromStack(h);
        h.Append(h.Create(OpCodes.Callvirt, setLabelSettingsOutlineSize));

        // settings.ShadowSize = Scale(settings.ShadowSize)
        h.Append(h.Create(OpCodes.Ldloc_2));
        h.Append(h.Create(OpCodes.Ldloc_2));
        h.Append(h.Create(OpCodes.Callvirt, getLabelSettingsShadowSize));
        EmitScaleIntFromStack(h);
        h.Append(h.Create(OpCodes.Callvirt, setLabelSettingsShadowSize));

        // lbl.SetLabelSettings(settings)
        h.Append(h.Create(OpCodes.Ldloc_1));
        h.Append(h.Create(OpCodes.Ldloc_2));
        h.Append(h.Create(OpCodes.Callvirt, setLabelSettings));

        h.Append(afterSettings);

        AddOverride(1, "font_size", "Label");
        h.Append(h.Create(OpCodes.Ret));
        h.Append(ck4);

        // return;
        h.Append(h.Create(OpCodes.Ret));

        nGame.Methods.Add(helper);

        // Create static helper: _FontScaleSubtree(Node node)
        var subtree = new MethodDefinition("_FontScaleSubtree",
            Mono.Cecil.MethodAttributes.Private | Mono.Cecil.MethodAttributes.Static | Mono.Cecil.MethodAttributes.HideBySig,
            module.ImportReference(typeof(void)));
        subtree.Parameters.Add(new ParameterDefinition("node", Mono.Cecil.ParameterAttributes.None, module.ImportReference(nodeType)));
        subtree.Body.InitLocals = true;
        subtree.Body.Variables.Add(new VariableDefinition(module.TypeSystem.Int32)); // loc0: childCount
        subtree.Body.Variables.Add(new VariableDefinition(module.TypeSystem.Int32)); // loc1: i

        var s = subtree.Body.GetILProcessor();
        var loopBody = s.Create(OpCodes.Nop);
        var loopCheck = s.Create(OpCodes.Nop);

        s.Append(s.Create(OpCodes.Ldarg_0));
        s.Append(s.Create(OpCodes.Call, helper));
        s.Append(s.Create(OpCodes.Ldarg_0));
        s.Append(s.Create(OpCodes.Ldc_I4_0));
        s.Append(s.Create(OpCodes.Callvirt, getChildCount));
        s.Append(s.Create(OpCodes.Stloc_0));
        s.Append(s.Create(OpCodes.Ldc_I4_0));
        s.Append(s.Create(OpCodes.Stloc_1));
        s.Append(s.Create(OpCodes.Br, loopCheck));

        s.Append(loopBody);
        s.Append(s.Create(OpCodes.Ldarg_0));
        s.Append(s.Create(OpCodes.Ldloc_1));
        s.Append(s.Create(OpCodes.Ldc_I4_0));
        s.Append(s.Create(OpCodes.Callvirt, getChild));
        s.Append(s.Create(OpCodes.Call, subtree));
        s.Append(s.Create(OpCodes.Ldloc_1));
        s.Append(s.Create(OpCodes.Ldc_I4_1));
        s.Append(s.Create(OpCodes.Add));
        s.Append(s.Create(OpCodes.Stloc_1));

        s.Append(loopCheck);
        s.Append(s.Create(OpCodes.Ldloc_1));
        s.Append(s.Create(OpCodes.Ldloc_0));
        s.Append(s.Create(OpCodes.Blt, loopBody));
        s.Append(s.Create(OpCodes.Ret));

        nGame.Methods.Add(subtree);

        // Hook: GetTree().NodeAdded += _FontScaleOnNodeAdded;
        var nodeAddedEvent = sceneTreeType.Events.First(e => e.Name == "NodeAdded");
        var addMethod = module.ImportReference(nodeAddedEvent.AddMethod);
        var handlerCtor = module.ImportReference(nodeAddedEvent.EventType.Resolve().Methods.First(m => m.IsConstructor && m.Parameters.Count == 2));

        var ril = nReady.Body.GetILProcessor();
        var rret = nReady.Body.Instructions.Last(i => i.OpCode == OpCodes.Ret);
        ril.InsertBefore(rret, ril.Create(OpCodes.Ldarg_0));
        ril.InsertBefore(rret, ril.Create(OpCodes.Call, getTree));
        ril.InsertBefore(rret, ril.Create(OpCodes.Ldnull));
        ril.InsertBefore(rret, ril.Create(OpCodes.Ldftn, helper));
        ril.InsertBefore(rret, ril.Create(OpCodes.Newobj, handlerCtor));
        ril.InsertBefore(rret, ril.Create(OpCodes.Callvirt, addMethod));
        ril.InsertBefore(rret, ril.Create(OpCodes.Ldarg_0));
        ril.InsertBefore(rret, ril.Create(OpCodes.Call, subtree));

        Console.WriteLine("PATCH 2: NGame._Ready SceneTree.NodeAdded hook + subtree scan"); patchCount++;
    }
}

// PATCH 3: GodotSharp.RichTextLabel — scale inline BBCode and direct font-size pushes
{
    var richTextLabel = godotSharp.MainModule.Types.FirstOrDefault(t => t.FullName == "Godot.RichTextLabel");
    if (richTextLabel != null)
    {
        var regexAsm = resolver.Resolve(godotSharp.MainModule.AssemblyReferences.First(r => r.Name == "System.Text.RegularExpressions"));
        var regexType = regexAsm.MainModule.Types.First(t => t.FullName == "System.Text.RegularExpressions.Regex");
        var matchType = regexAsm.MainModule.Types.First(t => t.FullName == "System.Text.RegularExpressions.Match");
        var captureType = regexAsm.MainModule.Types.First(t => t.FullName == "System.Text.RegularExpressions.Capture");
        var matchEvaluatorType = regexAsm.MainModule.Types.First(t => t.FullName == "System.Text.RegularExpressions.MatchEvaluator");

        var stringType = godotSharp.MainModule.TypeSystem.String.Resolve();
        var intType = godotSharp.MainModule.TypeSystem.Int32.Resolve();
        var objectType = godotSharp.MainModule.TypeSystem.Object.Resolve();

        var regexReplace = godotSharp.MainModule.ImportReference(regexType.Methods.First(m => m.Name == "Replace" && m.IsStatic && m.Parameters.Count == 3 && m.Parameters[0].ParameterType.FullName == "System.String" && m.Parameters[1].ParameterType.FullName == "System.String" && m.Parameters[2].ParameterType.FullName == "System.Text.RegularExpressions.MatchEvaluator"));
        var matchEvaluatorCtor = godotSharp.MainModule.ImportReference(matchEvaluatorType.Methods.First(m => m.IsConstructor && m.Parameters.Count == 2));
        var captureGetValue = godotSharp.MainModule.ImportReference(captureType.Methods.First(m => m.Name == "get_Value" && m.Parameters.Count == 0));
        var intParse = godotSharp.MainModule.ImportReference(intType.Methods.First(m => m.Name == "Parse" && m.IsStatic && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.String"));
        var objectToString = godotSharp.MainModule.ImportReference(objectType.Methods.First(m => m.Name == "ToString" && !m.IsStatic && m.Parameters.Count == 0));
        var stringIndexOf = godotSharp.MainModule.ImportReference(stringType.Methods.First(m => m.Name == "IndexOf" && m.Parameters.Count == 2 && m.Parameters[0].ParameterType.FullName == "System.String" && m.Parameters[1].ParameterType.FullName == "System.StringComparison"));

        var scaleIntMethod = richTextLabel.Methods.FirstOrDefault(m => m.Name == "__StsScaleInt" && m.Parameters.Count == 1);
        if (scaleIntMethod == null)
        {
            scaleIntMethod = new MethodDefinition("__StsScaleInt",
                Mono.Cecil.MethodAttributes.Private | Mono.Cecil.MethodAttributes.Static | Mono.Cecil.MethodAttributes.HideBySig,
                godotSharp.MainModule.TypeSystem.Int32);
            scaleIntMethod.Parameters.Add(new ParameterDefinition("value", Mono.Cecil.ParameterAttributes.None, godotSharp.MainModule.TypeSystem.Int32));
            richTextLabel.Methods.Add(scaleIntMethod);

            var il = scaleIntMethod.Body.GetILProcessor();
            var positive = il.Create(OpCodes.Nop);
            il.Append(il.Create(OpCodes.Ldarg_0));
            il.Append(il.Create(OpCodes.Ldc_I4_0));
            il.Append(il.Create(OpCodes.Bgt_S, positive));
            il.Append(il.Create(OpCodes.Ldarg_0));
            il.Append(il.Create(OpCodes.Ret));
            il.Append(positive);
            il.Append(il.Create(OpCodes.Ldarg_0));
            il.Append(il.Create(OpCodes.Conv_R8));
            il.Append(il.Create(OpCodes.Ldc_R8, scaleFactor));
            il.Append(il.Create(OpCodes.Mul));
            il.Append(il.Create(OpCodes.Ldc_R8, 0.5));
            il.Append(il.Create(OpCodes.Add));
            il.Append(il.Create(OpCodes.Conv_I4));
            il.Append(il.Create(OpCodes.Ret));
        }

        var scaleMatchMethod = richTextLabel.Methods.FirstOrDefault(m => m.Name == "__StsScaleMatch" && m.Parameters.Count == 1);
        if (scaleMatchMethod == null)
        {
            scaleMatchMethod = new MethodDefinition("__StsScaleMatch",
                Mono.Cecil.MethodAttributes.Private | Mono.Cecil.MethodAttributes.Static | Mono.Cecil.MethodAttributes.HideBySig,
                godotSharp.MainModule.TypeSystem.String);
            scaleMatchMethod.Parameters.Add(new ParameterDefinition("match", Mono.Cecil.ParameterAttributes.None, godotSharp.MainModule.ImportReference(matchType)));
            richTextLabel.Methods.Add(scaleMatchMethod);

            var il = scaleMatchMethod.Body.GetILProcessor();
            il.Append(il.Create(OpCodes.Ldarg_0));
            il.Append(il.Create(OpCodes.Callvirt, captureGetValue));
            il.Append(il.Create(OpCodes.Call, intParse));
            il.Append(il.Create(OpCodes.Call, scaleIntMethod));
            il.Append(il.Create(OpCodes.Box, godotSharp.MainModule.TypeSystem.Int32));
            il.Append(il.Create(OpCodes.Callvirt, objectToString));
            il.Append(il.Create(OpCodes.Ret));
        }

        var scaleBbcodeMethod = richTextLabel.Methods.FirstOrDefault(m => m.Name == "__StsScaleBbcode" && m.Parameters.Count == 1);
        if (scaleBbcodeMethod == null)
        {
            scaleBbcodeMethod = new MethodDefinition("__StsScaleBbcode",
                Mono.Cecil.MethodAttributes.Private | Mono.Cecil.MethodAttributes.Static | Mono.Cecil.MethodAttributes.HideBySig,
                godotSharp.MainModule.TypeSystem.String);
            scaleBbcodeMethod.Parameters.Add(new ParameterDefinition("text", Mono.Cecil.ParameterAttributes.None, godotSharp.MainModule.TypeSystem.String));
            scaleBbcodeMethod.Body.InitLocals = true;
            scaleBbcodeMethod.Body.Variables.Add(new VariableDefinition(godotSharp.MainModule.ImportReference(matchEvaluatorType))); // loc0 evaluator
            richTextLabel.Methods.Add(scaleBbcodeMethod);

            var il = scaleBbcodeMethod.Body.GetILProcessor();
            var notNull = il.Create(OpCodes.Nop);
            var skipFont = il.Create(OpCodes.Nop);
            var skipOutline = il.Create(OpCodes.Nop);

            il.Append(il.Create(OpCodes.Ldarg_0));
            il.Append(il.Create(OpCodes.Brtrue_S, notNull));
            il.Append(il.Create(OpCodes.Ldnull));
            il.Append(il.Create(OpCodes.Ret));

            il.Append(notNull);
            il.Append(il.Create(OpCodes.Ldnull));
            il.Append(il.Create(OpCodes.Ldftn, scaleMatchMethod));
            il.Append(il.Create(OpCodes.Newobj, matchEvaluatorCtor));
            il.Append(il.Create(OpCodes.Stloc_0));

            il.Append(il.Create(OpCodes.Ldarg_0));
            il.Append(il.Create(OpCodes.Ldstr, "font_size="));
            il.Append(il.Create(OpCodes.Ldc_I4_4)); // StringComparison.Ordinal
            il.Append(il.Create(OpCodes.Callvirt, stringIndexOf));
            il.Append(il.Create(OpCodes.Ldc_I4_0));
            il.Append(il.Create(OpCodes.Blt_S, skipFont));
            il.Append(il.Create(OpCodes.Ldarg_0));
            il.Append(il.Create(OpCodes.Ldstr, "(?<=\\bfont_size=)-?\\d+"));
            il.Append(il.Create(OpCodes.Ldloc_0));
            il.Append(il.Create(OpCodes.Call, regexReplace));
            il.Append(il.Create(OpCodes.Starg_S, scaleBbcodeMethod.Parameters[0]));

            il.Append(skipFont);
            il.Append(il.Create(OpCodes.Ldarg_0));
            il.Append(il.Create(OpCodes.Ldstr, "outline_size="));
            il.Append(il.Create(OpCodes.Ldc_I4_4)); // StringComparison.Ordinal
            il.Append(il.Create(OpCodes.Callvirt, stringIndexOf));
            il.Append(il.Create(OpCodes.Ldc_I4_0));
            il.Append(il.Create(OpCodes.Blt_S, skipOutline));
            il.Append(il.Create(OpCodes.Ldarg_0));
            il.Append(il.Create(OpCodes.Ldstr, "(?<=\\boutline_size=)-?\\d+"));
            il.Append(il.Create(OpCodes.Ldloc_0));
            il.Append(il.Create(OpCodes.Call, regexReplace));
            il.Append(il.Create(OpCodes.Starg_S, scaleBbcodeMethod.Parameters[0]));

            il.Append(skipOutline);
            il.Append(il.Create(OpCodes.Ldarg_0));
            il.Append(il.Create(OpCodes.Ret));
        }

        void PatchStringParam(string methodName, int parameterIndex)
        {
            var method = richTextLabel.Methods.FirstOrDefault(m => m.Name == methodName && m.Parameters.Count > parameterIndex);
            if (method == null)
            {
                return;
            }

            var il = method.Body.GetILProcessor();
            var first = method.Body.Instructions[0];
            il.InsertBefore(first, il.Create(OpCodes.Ldarg, method.Parameters[parameterIndex]));
            il.InsertBefore(first, il.Create(OpCodes.Call, scaleBbcodeMethod));
            il.InsertBefore(first, il.Create(OpCodes.Starg_S, method.Parameters[parameterIndex]));
            godotPatchCount++;
            Console.WriteLine($"PATCH 3: Godot.RichTextLabel.{methodName} string arg");
        }

        void PatchIntParam(string methodName, int parameterIndex, int? parameterCount = null)
        {
            var method = richTextLabel.Methods.FirstOrDefault(m =>
                m.Name == methodName &&
                m.Parameters.Count > parameterIndex &&
                (parameterCount == null || m.Parameters.Count == parameterCount.Value));
            if (method == null)
            {
                return;
            }

            var il = method.Body.GetILProcessor();
            var first = method.Body.Instructions[0];
            il.InsertBefore(first, il.Create(OpCodes.Ldarg, method.Parameters[parameterIndex]));
            il.InsertBefore(first, il.Create(OpCodes.Call, scaleIntMethod));
            il.InsertBefore(first, il.Create(OpCodes.Starg_S, method.Parameters[parameterIndex]));
            godotPatchCount++;
            Console.WriteLine($"PATCH 3: Godot.RichTextLabel.{methodName} int arg {parameterIndex}");
        }

        PatchStringParam("SetText", 0);
        PatchStringParam("ParseBbcode", 0);
        PatchStringParam("AppendText", 0);
        PatchIntParam("PushFont", 1, parameterCount: 2);
        PatchIntParam("PushFontSize", 0, parameterCount: 1);
        PatchIntParam("PushOutlineSize", 0, parameterCount: 1);
        PatchIntParam("PushDropcap", 2, parameterCount: 7);
        PatchIntParam("PushDropcap", 5, parameterCount: 7);
    }
}

var patchedPath = dllPath + ".patched";
assembly.Write(patchedPath);
File.Copy(patchedPath, dllPath, overwrite: true);
File.Delete(patchedPath);

var godotPatchedPath = godotSharpPath + ".patched";
godotSharpTarget.Write(godotPatchedPath);
File.Copy(godotPatchedPath, godotSharpPath, overwrite: true);
File.Delete(godotPatchedPath);

Console.WriteLine($"\nDone! {patchCount} sts2 patches and {godotPatchCount} GodotSharp patches at {scaleFactor:F2}x");
return 0;
