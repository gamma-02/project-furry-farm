@tool
extends Node3D

@export
var placedSprites: bool = false

@onready
var sprite: Sprite3D = $origin_sprite

var sprites = []

#distance between placed sprites
@export var dist_between_sprites: float = 0.5

#This describes the number of sprites per side of the cube centering around 0, 0
@export var sprite_field_side_sprite_count: Vector3i = Vector3i(2, 2, 2)

@export_range(0.0, 1.0) var ground_level: float = 0.5

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	# Immediately return if we've already placed all the sprites
	if placedSprites:
		pass
	
	addSprites()
	
	placedSprites = true;
	
	return

@export
var replaceSprites: bool = false

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	if replaceSprites:
		removeSprites()
		addSprites()
		replaceSprites = false
	
	for spriteObj in sprites:
		var sprite: Sprite3D = spriteObj as Sprite3D
		sprite.ground_level = ground_level
	
	pass

func addSprites() -> void:
	var size = sprite_field_side_sprite_count;
	for x in range((-size.x / 2) - ((size.x % 2) - 1), size.x / 2 + 1):
		for y in range((-size.y / 2) - ((size.y % 2 ) - 1), size.y / 2 + 1):
			for z in range((-size.z / 2) - ((size.z % 2) - 1), size.z / 2 + 1):
				var pos = Vector3(x, y, z)
				
				#skip the sprite at 0, 0 (we already have a sprite there)
				if pos == Vector3.ZERO:
					continue
				
				#ok, now we need to transform pos so that it leaves the proper space between sprites.
				#it is added such that there is 1 unit between sprites by default. I think i can
				#just multiply by the dist_between_sprites here? since the origin is at the center?
				pos *= dist_between_sprites
				
				var new_sprite = sprite.duplicate()
				add_child(new_sprite)
				new_sprite.position = pos
				sprites.append(new_sprite)
	
	return

func removeSprites() -> void:
	for sprite in sprites:
		(sprite as Sprite3D).queue_free()
	
	sprites.clear()
	
	return
