shader_type canvas_item;

uniform sampler2D palette_texture;
uniform int palette;
uniform sampler2D dither_texture : repeat_enable, filter_nearest;

uniform int bit_depth;
uniform float contrast;
uniform float luminosity_offset;
uniform int dither_size;

uniform vec2 dither_offset;

void fragment() {
	
	int colors = textureSize(palette_texture, 0).x;
	
	// sample the screen texture at the desired output resolution (according to u_dither_size)
	// this will effectively pixelate the resulting output
	vec2 screen_size = vec2(textureSize(TEXTURE, 0)) / float(dither_size);
	vec2 screen_sample_uv = floor(UV * screen_size) / screen_size;
	vec3 outputColor = texture(TEXTURE, screen_sample_uv).rgb;

	float luminosity = (outputColor.r * 0.299) + (outputColor.g * 0.587) + (outputColor.b * 0.114);
	
	luminosity = (luminosity - 0.5 + luminosity_offset) * contrast + 0.5;
	luminosity = clamp(luminosity, 0.0, 1.0);
	
	float bits = float(bit_depth);
	luminosity = floor(luminosity * bits) / bits;
	
	vec2 dither_texel_size = vec2(1.0, 1.0) / vec2(textureSize(dither_texture, 0));
	
	vec2 noise_uv = UV * dither_texel_size * vec2(screen_size);
	noise_uv -= dither_offset * dither_texel_size;
	
	luminosity = max(luminosity - 0.00001, 0.0); // makes sure our floor calculation below behaves when lum == 1.0
	float lum_lower = floor(luminosity * float(colors)) / float(colors);
	float lum_upper = (floor(luminosity * float(colors)) + 1.0) / float(colors);
	float lum_scaled = luminosity * float(colors) - floor(luminosity * float(colors)); // calculates where lum lies between the upper and lower bound
	
	vec3 threshold = texture(dither_texture, noise_uv).rgb;
	float threshold_lum = dot(threshold, vec3(0.299f, 0.587f, 0.114f));
	threshold_lum = threshold_lum * 0.8 + 0.1;
	
	float ramp_value = lum_scaled < threshold_lum ? 0.0 : 1.0f;
	
	float color_sample = mix(lum_lower, lum_upper, ramp_value);
	
	vec3 output = texture(palette_texture, vec2(floor(color_sample * float(colors)) / float(colors) + 0.5 / float(colors), (0.5 + float(palette)) / float(textureSize(palette_texture, 0).y))).rgb;
	
	COLOR.rgb = output;
	
}
