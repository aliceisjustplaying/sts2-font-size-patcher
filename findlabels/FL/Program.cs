using Mono.Cecil;
using Mono.Cecil.Cil;

if (args.Length < 1)
{
    Console.WriteLine("Usage: dotnet run -- <assembly-path>");
    return;
}

var asmPath = args[0];
var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(asmPath))!);

var asm = AssemblyDefinition.ReadAssembly(
    asmPath,
    new ReaderParameters { AssemblyResolver = resolver });

var targets = new HashSet<string>
{
    "MegaCrit.Sts2.addons.mega_text.MegaLabel::_Ready",
    "MegaCrit.Sts2.addons.mega_text.MegaRichTextLabel::_Ready",
    "MegaCrit.Sts2.Core.Nodes.NGame::_FontScaleOnNodeAdded",
    "MegaCrit.Sts2.Core.Nodes.NGame::_FontScaleSubtree",
    "Godot.RichTextLabel::__StsScaleInt",
    "Godot.RichTextLabel::__StsScaleMatch",
    "Godot.RichTextLabel::__StsScaleBbcode",
    "Godot.RichTextLabel::SetText",
    "Godot.RichTextLabel::ParseBbcode",
    "Godot.RichTextLabel::AppendText",
    "Godot.RichTextLabel::PushFont",
    "Godot.RichTextLabel::PushFontSize",
    "Godot.RichTextLabel::PushOutlineSize",
    "Godot.RichTextLabel::PushDropcap",
    "MegaCrit.Sts2.Core.Nodes.Debug.NDebugInfoLabelManager::UpdateText",
    "MegaCrit.Sts2.Core.Nodes.Debug.NDebugInfoLabelManager::_Ready",
    "MegaCrit.Sts2.Core.Nodes.Debug.NDebugInfoLabelManager::_Input",
    "MegaCrit.Sts2.Core.Nodes.Debug.NDebugInfoLabelManager::SetCommitIdInEditor",
    "MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen.NRunHistory::LoadTimeDetails",
    "MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NContinueRunInfo::ShowInfo"
};

MethodDefinition? FindMethod(string fullName)
{
    foreach (var type in asm.MainModule.Types)
    {
        foreach (var method in type.Methods)
        {
            if ($"{type.FullName}::{method.Name}" == fullName)
            {
                return method;
            }
        }
    }

    return null;
}

foreach (var fullName in targets)
{
    var method = FindMethod(fullName);
    if (method == null)
    {
        continue;
    }

    Console.WriteLine($"METHOD {fullName}");
    foreach (var instruction in method.Body.Instructions)
    {
        var operand = instruction.Operand switch
        {
            MethodReference mr => mr.FullName,
            FieldReference fr => fr.FullName,
            TypeReference tr => tr.FullName,
            ParameterDefinition pd => pd.Name,
            VariableDefinition vd => $"V_{method.Body.Variables.IndexOf(vd)}:{vd.VariableType.FullName}",
            string s => $"\"{s}\"",
            Instruction target => $"IL_{target.Offset:x4}",
            _ => instruction.Operand?.ToString()
        };

        Console.WriteLine($"  IL_{instruction.Offset:x4}: {instruction.OpCode} {operand}".TrimEnd());
    }

    Console.WriteLine();
}
