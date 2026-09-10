#version 330 core
out vec4 FragColor;

in vec2 TexCoords;

uniform sampler2D screenTexture;

void main()
{
    vec3 col = texture(screenTexture, vec2(1.0 - TexCoords.x, TexCoords.y)).rgb;
    FragColor = vec4(col, 1.0);
} 
