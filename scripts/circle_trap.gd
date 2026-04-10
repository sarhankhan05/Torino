extends Area2D

@onready var anim = $AnimatedSprite2D
@onready var hitbox = $CollisionShape2D

@export var small_scale := 1.0
@export var large_scale := 1.5


func _ready():
	anim.frame_changed.connect(_on_frame_changed)
	body_entered.connect(_on_body_entered)


func _on_frame_changed():

	var frame = anim.frame

	# Frames 2 → 9 use larger hitbox
	if frame >= 2 and frame <= 9:
		hitbox.scale = Vector2(large_scale, large_scale)
	else:
		hitbox.scale = Vector2(small_scale, small_scale)


func _on_body_entered(body):
	if body.has_method("die"):
		body.die()
