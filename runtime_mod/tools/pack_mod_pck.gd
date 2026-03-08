extends SceneTree

func _init() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() < 2:
		push_error("Usage: --script pack_mod_pck.gd -- <output.pck> <mod_manifest.json>")
		quit(1)
		return

	var output_path: String = args[0]
	var manifest_path: String = args[1]

	var packer := PCKPacker.new()

	var err := packer.pck_start(output_path)
	if err != OK:
		push_error("pck_start failed: %s" % err)
		quit(err)
		return

	err = packer.add_file("res://mod_manifest.json", manifest_path)
	if err != OK:
		push_error("add_file failed: %s" % err)
		quit(err)
		return

	err = packer.flush(true)
	if err != OK:
		push_error("flush failed: %s" % err)
		quit(err)
		return

	quit(0)
