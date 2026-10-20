shader_type spatial;
// Prevent Godot's standard PBR lighting from fighting your retro look
render_mode diffuse_toon, specular_toon;

// Per-material properties.
uniform sampler2D albedo_texture : source_color;
uniform vec4 material_tint : source_color = vec4(1.0);
uniform vec3 ambient_tint : source_color = vec3(0.72, 0.78, 0.90);
uniform vec3 specular_color : source_color = vec3(1.0);
uniform float specular_strength : hint_range(0.0, 1.0) = 0.22;
uniform float phong_exponent : hint_range(2.0, 64.0) = 24.0;

// Shared project-wide controls.
global uniform float retro_shadow_threshold;
global uniform float retro_edge_softness;
global uniform float retro_ambient_fill;
global uniform float retro_specular_multiplier;
global uniform float retro_diffuse_multiplier;

void fragment() {
    vec3 tex_color = texture(albedo_texture, UV).rgb;
    ALBEDO = tex_color * material_tint.rgb;

    // In a custom lighting workflow, EMISSION handles your ambient base floor safely
    EMISSION = ALBEDO * ambient_tint * retro_ambient_fill;
}

void light() {
    float shadow = ATTENUATION;

    if (LIGHT_IS_DIRECTIONAL) {
        float softness = retro_edge_softness;
        shadow = step(retro_shadow_threshold, ATTENUATION);
    }

    float ndotl = max(dot(NORMAL, LIGHT), 0.0);

    // FIX: Instead of re-multiplying ALBEDO directly here, modulate the LIGHT attenuation buffer.
    // This allows Godot to safely blend multi-light contributions without compound color scaling.
    DIFFUSE_LIGHT += LIGHT_COLOR * ndotl * retro_diffuse_multiplier * shadow;

    if (ndotl > 0.0 && specular_strength > 0.001 && retro_specular_multiplier > 0.001) {
        vec3 reflected = reflect(-LIGHT, NORMAL);
        float rv = max(dot(reflected, VIEW), 0.0);

        float specular = pow(rv, phong_exponent)
            * specular_strength
            * retro_specular_multiplier
            * SPECULAR_AMOUNT
            * shadow;

        SPECULAR_LIGHT += LIGHT_COLOR * specular * specular_color;
    }
}
