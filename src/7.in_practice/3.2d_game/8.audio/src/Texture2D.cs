/*******************************************************************
** This code is part of Breakout.
**
** Breakout is free software: you can redistribute it and/or modify
** it under the terms of the CC BY 4.0 license as published by
** Creative Commons, either version 4 of the License, or (at your
** option) any later version.
******************************************************************/

using Silk.NET.OpenGL;

namespace Breakout.src;

// A Texture2D é capaz de armazenar e configurar uma textura no OpenGL.
// Ela também disponibiliza funções utilitárias para facilitar o gerenciamento.
public class Texture2D
{
    private GL _gl;

    // armazena o ID do objeto de textura, usado em todas as operações de textura para referenciar essa textura específica
    public uint ID;

    //dimensões da imagem da textura
    public uint Widht, Height; // largura e altura da imagem carregada em pixels

    // Formato da textura
    public InternalFormat Internal_Format; // formato do objeto de textura
    public PixelFormat Image_Format; // formato da imagem carregada

    // configuração de textura
    public uint Wrap_S; // modo de envelopamento no eixo S
    public uint Wrap_T; // modo de envelopamento no eixo T
    public uint Filter_Min; // modo de filtragem se pixels da textura < pixels da tela
    public uint Filter_Max; // modo de filtragem se pixels da textura > pixels da tela

    // construtor (define os modos de textura padrão)
    public Texture2D(GL gl)
    {
        _gl = gl;

        Widht = 0;
        Height = 0;

        Internal_Format = InternalFormat.Rgb;
        Image_Format = PixelFormat.Rgb;

        Wrap_S = (int)TextureWrapMode.Repeat;
        Wrap_T = (int)TextureWrapMode.Repeat;
        Filter_Min = (int)TextureMinFilter.Linear;
        Filter_Max = (int)TextureMagFilter.Linear;

        _gl.GenTextures(1, out ID);
    }

    // gera uma textura a partir de dados de imagem
    public unsafe void Generate(uint width, uint height, byte[]? data)
    {
        Widht = width;
        Height = height;

        // criar textura
        _gl.BindTexture(TextureTarget.Texture2D, ID);
        fixed (byte* ptr = data)
        {
            _gl.TexImage2D(TextureTarget.Texture2D, 0, Internal_Format, width, height, 0, Image_Format, PixelType.UnsignedByte, ptr);
        }

        // definir modos de wrap e filtro da textura
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, Wrap_S);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, Wrap_T);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, Filter_Min);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, Filter_Max);

        // desvincular textura
        _gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    // vincula a textura como o objeto de textura GL_TEXTURE_2D ativo no momento
    public void Bind()
    {
        _gl.BindTexture(TextureTarget.Texture2D, ID);
    }
}
