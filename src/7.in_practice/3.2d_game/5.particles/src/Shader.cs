/*******************************************************************
** This code is part of Breakout.
**
** Breakout is free software: you can redistribute it and/or modify
** it under the terms of the CC BY 4.0 license as published by
** Creative Commons, either version 4 of the License, or (at your
** option) any later version.
******************************************************************/

using System.Numerics;
using Silk.NET.OpenGL;

namespace Breakout.src;

// Objeto de shader de propósito geral. Compila a partir de um arquivo, gera
// mensagens de erro de compilação/vinculação e disponibiliza várias funções
// utilitárias para facilitar o gerenciamento.
public class Shader
{
    private GL _gl;

    // estado
    public uint ID;

    // construtor
    public Shader(GL gl)
    {
        _gl = gl;
    }

    // define o shader atual como ativo
    public Shader Use()
    {
        _gl.UseProgram(ID);
        return this;
    }

    // compila o shader a partir do código-fonte fornecido
    public void Compile(string vertexSource, string fragmentSource, string? geometrySource = null)
    {
        uint vertex, fragment, geometry = 0;

        // vertex Shader
        vertex = _gl.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vertex, vertexSource);
        _gl.CompileShader(vertex);
        CheckCompileErrors(vertex, "VERTEX");

        // fragment Shader
        fragment = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(fragment, fragmentSource);
        _gl.CompileShader(fragment);
        CheckCompileErrors(fragment, "FRAGMENT");

        // se o código-fonte do shader de geometria for fornecido, compile também o shader de geometria
        if (geometrySource != null)
        {
            geometry = _gl.CreateShader(ShaderType.GeometryShader);
            _gl.ShaderSource(geometry, geometrySource);
            _gl.CompileShader(geometry);
            CheckCompileErrors(geometry, "GEOMETRY");
        }

        // shader program
        ID = _gl.CreateProgram();

        _gl.AttachShader(ID, vertex);
        _gl.AttachShader(ID, fragment);
        if (geometrySource != null)
        {
            _gl.AttachShader(ID, geometry);
        }

        _gl.LinkProgram(ID);
        CheckCompileErrors(ID, "PROGRAM");

        // exclua os shaders, pois eles já estão vinculados ao nosso programa e não são mais necessários
        _gl.DeleteShader(vertex);
        _gl.DeleteShader(fragment);
        if (geometrySource != null)
        {
            _gl.DeleteShader(geometry);
        }
    }

    // funções utilitárias
    public void SetFloat(string name, float value, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = _gl.GetUniformLocation(ID, name);
        _gl.Uniform1(location, value);
    }

    public void SetInteger(string name, int value, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = _gl.GetUniformLocation(ID, name);
        _gl.Uniform1(location, value);
    }

    public void SetVector2f(string name, Vector2 value, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = _gl.GetUniformLocation(ID, name);
        _gl.Uniform2(location, value);
    }

    public void SetVector2f(string name, float x, float y, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = _gl.GetUniformLocation(ID, name);
        _gl.Uniform2(location, x, y);
    }

    public void SetVector3f(string name, Vector3 value, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = _gl.GetUniformLocation(ID, name);
        _gl.Uniform3(location, value);
    }

    public void SetVector3f(string name, float x, float y, float z, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = _gl.GetUniformLocation(ID, name);
        _gl.Uniform3(location, x, y, z);
    }

    public void SetVector4f(string name, Vector4 value, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = _gl.GetUniformLocation(ID, name);
        _gl.Uniform4(location, value);
    }

    public void SetVector4f(string name, float x, float y, float z, float w, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = _gl.GetUniformLocation(ID, name);
        _gl.Uniform4(location, x, y, z, w);
    }

    public unsafe void SetMatrix4(string name, Matrix4x4 matrix, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = _gl.GetUniformLocation(ID, name);
        _gl.UniformMatrix4(location, 1, false, (float*)&matrix);
    }

    // verifica se a compilação ou a vinculação falharam e, em caso afirmativo, imprime os logs de erro
    private void CheckCompileErrors(uint shader, string type)
    {
        int success;
        string infoLog;

        if (type != "PROGRAM")
        {
            _gl.GetShader(shader, ShaderParameterName.CompileStatus, out success);
            if (success == 0)
            {
                _gl.GetShaderInfoLog(shader, out infoLog);
                Console.WriteLine(
                    "| ERROR::SHADER: Compile-time error: Type: " + type + "\n" +
                    infoLog + "\n" +
                    " -- --------------------------------------------------- -- "
                );
            }
        }
        else
        {
            _gl.GetProgram(shader, ProgramPropertyARB.LinkStatus, out success);
            if (success == 0)
            {
                _gl.GetProgramInfoLog(shader, out infoLog);
                Console.WriteLine(
                    "| ERROR::Shader: Link-time error: Type: " + type + "\n" +
                    infoLog + "\n" +
                    " -- --------------------------------------------------- -- "
                );
            }
        }
    }
}
