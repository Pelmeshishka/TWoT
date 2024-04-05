shader_type spatial;

uniform sampler2DArray textures : source_color, filter_nearest;
uniform sampler2D lights : source_color, filter_nearest;
//uniform sampler2D DEPTH_TEXTURE: hint_depth_texture, filter_linear_mipmap;
//uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;


void fragment() {
	
	vec4 color = texture(textures, vec3(UV.x, UV.y, UV2.x));
	if (UV2.y < 0.0) 
	{
		ALBEDO = color.rgb;
	} 
	else 
	{
		vec4 color2 = texture(textures, vec3(UV.x, UV.y, UV2.y));
		ALBEDO = mix(color, color2, color.a).rgb;
	}
	
	//vec3 world_pos = (MODEL_MATRIX*INV_VIEW_MATRIX * vec4(VERTEX-NORMAL*0.000001, 1.0)).xyz-NODE_POSITION_WORLD;
	//vec3 normal = normalize(mat3(INV_VIEW_MATRIX) * NORMAL);
	//vec3 blockPos = floor(world_pos+normal);
	
	//float blockIndex = (blockPos.x + blockPos.y * 16.0 + blockPos.z * 16.0 * 16.0);
	//vec2 pixel_uv = vec2(blockIndex/4096.0, 0.5);
	//vec4 pixel_color = texture(lights, pixel_uv);
	//vec3 light = clamp(pixel_color.rgb + vec3(0.0/15.0), vec3(0.0), vec3(1.0));
	//ALBEDO = light;
	ALBEDO = vec3(1);
}