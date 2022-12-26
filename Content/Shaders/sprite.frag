#version 450

layout(set = 1, binding = 0) uniform sampler2D uniformTexture;

layout(location = 0) in vec2 texCoord;
layout(location = 1) in vec4 color;

layout(location = 0) out vec4 fragColor;

void main()
{
    fragColor = texture(uniformTexture, texCoord) * color;
}
