/*******************************************************************
** This code is part of Breakout.
**
** Breakout is free software: you can redistribute it and/or modify
** it under the terms of the CC BY 4.0 license as published by
** Creative Commons, either version 4 of the License, or (at your
** option) any later version.
******************************************************************/

using Silk.NET.GLFW;
using Silk.NET.OpenGL;

namespace Breakout.src;

public class Program
{
    private static Glfw _glfw = null!;
    private static GL _gl = null!;

    // A largura da tela
    private const int SCREEN_WIDTH = 800;

    // A altura da tela
    private const int SCREEN_HEIGHT = 600;

    private static Game Breakout = new Game(SCREEN_WIDTH, SCREEN_HEIGHT);

    private static unsafe void Main(string[] args)
    {
        _glfw = Glfw.GetApi();
        _gl = GL.GetApi(_glfw.GetProcAddress);

        _glfw.Init();
        _glfw.WindowHint(WindowHintInt.ContextVersionMajor, 3);
        _glfw.WindowHint(WindowHintInt.ContextVersionMinor, 3);
        _glfw.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);

        if (OperatingSystem.IsMacOS())
        {
            _glfw.WindowHint(WindowHintBool.OpenGLForwardCompat, true);
        }

        _glfw.WindowHint(WindowHintBool.Resizable, false);

        WindowHandle* window = _glfw.CreateWindow(SCREEN_WIDTH, SCREEN_HEIGHT, "Breakout", null, null);

        // Obtém o tamanho da janela passado para glfwCreateWindow
        _glfw.GetWindowSize(window, out int pWidth, out int pHeight);

        // Obtém a resolução do monitor principal
        VideoMode* vidmode = _glfw.GetVideoMode(_glfw.GetPrimaryMonitor());

        // Centralizar a janela
        _glfw.SetWindowPos(
            window,
            (vidmode->Width - pWidth) / 2,
            (vidmode->Height - pHeight) / 2
        );

        _glfw.MakeContextCurrent(window);

        _glfw.SetKeyCallback(window, KeyCallback);
        _glfw.SetFramebufferSizeCallback(window, FramebufferSizeCallback);

        // Configuração do OpenGL
        // --------------------------------------------------
        _gl.Viewport(0, 0, (uint)SCREEN_WIDTH, (uint)SCREEN_HEIGHT);
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // inicializar o jogo
        // --------------------------------------------------
        Breakout.Init(_gl);

        // variáveis ​​de deltaTime
        // --------------------------------------------------
        float deltaTime = 0.0f;
        float lastFrame = 0.0f;

        while (!_glfw.WindowShouldClose(window))
        {
            // calcular o delta de tempo
            // --------------------------------------------------
            float currentFrame = (float)_glfw.GetTime();
            deltaTime = currentFrame - lastFrame;
            lastFrame = currentFrame;

            _glfw.PollEvents();

            // gerenciar a entrada do usuário
            // --------------------------------------------------
            Breakout.ProcessInput(deltaTime);

            // atualizar estado do jogo
            // --------------------------------------------------
            Breakout.Update(deltaTime);

            // render
            // --------------------------------------------------
            _gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            _gl.Clear(ClearBufferMask.ColorBufferBit);

            Breakout.Render(_glfw);

            _glfw.SwapBuffers(window);
        }

        // exclui todos os recursos carregados usando o gerenciador de recursos
        // --------------------------------------------------
        ResourceManager.Clear(_gl);
        Breakout.Dispose();

        _glfw.Terminate();
    }

    private static unsafe void KeyCallback(WindowHandle* window, Keys key, int scanCode, InputAction action, KeyModifiers mods)
    {
        // quando o usuário pressiona a tecla Esc, definimos a propriedade WindowShouldClose como true, fechando o aplicativo
        if (key == Keys.Escape && action == InputAction.Press)
        {
            _glfw.SetWindowShouldClose(window, true);
        }

        if (key >= 0 && key < (Keys)1024)
        {
            if (action == InputAction.Press)
            {
                Breakout.Keys[(int)key] = true;
            }
            else if (action == InputAction.Release)
            {
                Breakout.Keys[(int)key] = false;
                Breakout.KeysProcessed[(int)key] = false;
            }
        }
    }

    private static unsafe void FramebufferSizeCallback(WindowHandle* window, int width, int height)
    {
        // certifique-se de que a viewport corresponda às novas dimensões da janela; observe que a largura e
        // a altura serão significativamente maiores do que as especificadas em telas Retina.
        _gl.Viewport(0, 0, (uint)width, (uint)height);
    }
}
