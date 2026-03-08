extends SceneTree

func _init() -> void:
	var targets := [
		"res://mod_manifest.json",
		"res://project.binary",
		"res://.godot/global_script_class_cache.cfg"
	]

	for path in targets:
		print("%s => %s" % [path, FileAccess.file_exists(path)])

	quit(0)
