#version 330 core

// Fragment shader para desenhar um círculo sobre um quad.
// O quad deve fornecer coordenadas UV no intervalo [0, 1].
in vec2 TexCoords;

out vec4 FragColor;

uniform vec4 circleColor;
uniform float radius;

void main()
{
    // Centraliza as coordenadas e transforma o intervalo para [-1, 1].
    vec2 position = TexCoords * 2.0 - 1.0;
    float distanceFromCenter = length(position);

    // Usa derivadas da tela para manter a borda suave em diferentes resoluções.
    float edge = fwidth(distanceFromCenter);
    float alpha = 1.0 - smoothstep(radius - edge, radius + edge, distanceFromCenter);

    if (alpha <= 0.0)
        discard;

    FragColor = vec4(circleColor.rgb, circleColor.a * alpha);
}
