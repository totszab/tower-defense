shader_type canvas_item;

uniform vec4 color_a : source_color = vec4(0.04, 0.07, 0.16, 1.0);
uniform vec4 color_b : source_color = vec4(0.09, 0.13, 0.28, 1.0);
uniform float speed : hint_range(0.0, 2.0) = 0.12;

void fragment() {
	float t = TIME * speed;
	float wave = sin(UV.x * 3.0 + t) * 0.5 + 0.5;
	wave *= sin(UV.y * 2.2 - t * 0.8) * 0.5 + 0.5;
	COLOR = mix(color_a, color_b, wave);
}
