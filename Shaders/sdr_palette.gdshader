shader_type canvas_item;

uniform sampler2D palette_texture;
uniform int palette;
uniform sampler2D bayer_texture : repeat_enable, filter_nearest;
uniform sampler2D bluenoise_texture : repeat_enable, filter_nearest;
uniform float cohesiveness;
uniform float contrast;
uniform float luminosity_offset;
uniform int dither_size;
uniform float warble_speed;
uniform float warble_intensity;
uniform vec2 warble_vector;

uniform vec2 dither_offset;

void fragment() {

	int colors = textureSize(palette_texture, 0).x;

	// sample the screen texture at the desired output resolution (according to u_dither_size)
	// this will effectively pixelate the resulting output
	vec2 screen_size = vec2(textureSize(TEXTURE, 0)) / float(dither_size);
	vec2 screen_sample_uv = floor(UV * screen_size) / screen_size;
	vec3 outputColor = texture(TEXTURE, screen_sample_uv).rgb;

	float luminosity = (outputColor.r * 0.299) + (outputColor.g * 0.587) + (outputColor.b * 0.114);

	luminosity = (luminosity - 0.5) * contrast + 0.5;
	luminosity += luminosity_offset;
	luminosity = clamp(luminosity, 0.0, 1.0);

	bool useBlue = outputColor.b - outputColor.r > 0.01;

	vec2 bayer_texel_size = vec2(1.0, 1.0) / vec2(textureSize(bayer_texture, 0));
	vec2 bluenoise_texel_size = vec2(1.0, 1.0) / vec2(textureSize(bluenoise_texture, 0));

	vec2 dither_texel_size = useBlue ? bluenoise_texel_size : bayer_texel_size;

	vec2 noise_uv = UV * dither_texel_size * vec2(screen_size);
	noise_uv -= dither_offset * dither_texel_size;
	noise_uv += TIME * warble_speed * warble_vector * dither_texel_size;

	float lum_lower = floor(luminosity * float(colors)) / float(colors);
	float lum_upper = (floor(luminosity * float(colors)) + 1.0) / float(colors);
	float lum_scaled = luminosity * float(colors) - floor(luminosity * float(colors)); // calculates where lum lies between the upper and lower bound

	vec3 bayer_threshold = texture(bayer_texture, noise_uv).rgb;
	vec3 bluenoise_threshold = texture(bluenoise_texture, noise_uv).rgb;
	float threshold_lum = dot(useBlue ? bluenoise_threshold : bayer_threshold, vec3(0.299f, 0.587f, 0.114f));
	threshold_lum = threshold_lum * (1.0 - cohesiveness / 2.0) + cohesiveness / 2.0;

	float ramp_value = lum_scaled < threshold_lum ? 0.0 : 1.0f;

	float color_sample = mix(lum_lower, lum_upper, ramp_value);

	vec3 output = texture(palette_texture, vec2(floor(color_sample * float(colors) + 0.5) / float(colors), (0.5 + float(palette)) / float(textureSize(palette_texture, 0).y))).rgb;

	COLOR.rgb = output;

}
