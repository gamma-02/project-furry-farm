@tool
extends CollisionShape3D

var _parentMeshBuilder: MeshBuilder;

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	_parentMeshBuilder = (get_parent().get_parent() as MeshBuilder)
	
	return


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	if _parentMeshBuilder.CollisionMeshDirty: 
		shape = _parentMeshBuilder.CollisionMesh
		_parentMeshBuilder.CollisionMeshDirty = false
	
	return
