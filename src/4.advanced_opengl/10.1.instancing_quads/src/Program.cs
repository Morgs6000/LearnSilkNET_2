using System.Numerics;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;
using StbImageSharp;

namespace LearnSilkNET.src;

public class Program
{
    private static Glfw _glfw = null!;
    private static GL _gl = null!;

    // configurações
    private const uint SCR_WIDTH = 800;
    private const uint SCR_HEIGHT = 600;

    private unsafe static void Main(string[] args)
    {
        _glfw = Glfw.GetApi();
        _gl = GL.GetApi(_glfw.GetProcAddress);

        // glfw: inicializar e configurar
        // --------------------------------------------------
        _glfw.Init();
        _glfw.WindowHint(WindowHintInt.ContextVersionMajor, 3);
        _glfw.WindowHint(WindowHintInt.ContextVersionMinor, 3);
        _glfw.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);

        if (OperatingSystem.IsMacOS())
        {
            _glfw.WindowHint(WindowHintBool.OpenGLForwardCompat, true);
        }

        // criação da janela glfw
        // --------------------------------------------------
        WindowHandle* window = _glfw.CreateWindow((int)SCR_WIDTH, (int)SCR_HEIGHT, "Learn Silk.NET", null, null);

        if (window == null)
        {
            Console.WriteLine("Falha ao criar a janela Silk.NET");
            _glfw.Terminate();
        }

        // Obtém o tamanho da janela passado para glfwCreateWindow
        _glfw.GetWindowSize(window, out int pWidth, out int pHeight);

        // Obtém a resolução do monitor principal
        var vidmode = _glfw.GetVideoMode(_glfw.GetPrimaryMonitor());

        // Centralizar a janela
        _glfw.SetWindowPos(
            window,
            (vidmode->Width - pWidth) / 2,
            (vidmode->Height - pHeight) / 2
        );

        _glfw.MakeContextCurrent(window);

        // configurar estado global do OpenGL
        // --------------------------------------------------
        _gl.Enable(EnableCap.DepthTest);

        // construir e compilar nosso programa de shader
        // --------------------------------------------------
        Shader shader = new Shader(_gl, "src/instancing.vs", "src/instancing.fs");
        
        // gerar uma lista de 100 localizações de quad/vetores de translação
        // --------------------------------------------------
        Vector2[] translations = new Vector2[100];
        int index = 0;
        float offset = 0.1f;

        for (int y = -10; y < 10; y += 2)
        {
            for (int x = -10; x < 10; x += 2)
            {
                Vector2 translation;
                translation.X = (float)x / 10.0f + offset;
                translation.Y = (float)y / 10.0f + offset;

                translations[index++] = translation;
            }
        }

        // armazena dados da instância em um buffer de array
        // --------------------------------------------------
        uint instanceVBO;

        _gl.GenBuffers(1, out instanceVBO);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, instanceVBO);
        fixed (Vector2* buf = translations)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(sizeof(Vector2) * 100), buf, BufferUsageARB.StaticDraw);
        }
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        // configurar dados de vértice (e buffer(s)) e configurar atributos de vértice
        // --------------------------------------------------
        float[] quadVertices =
        {
            // positions      // colors
            -0.05f, -0.05f,   1.0f, 0.0f, 0.0f,
             0.05f, -0.05f,   0.0f, 1.0f, 0.0f,
             0.05f,  0.05f,   0.0f, 0.0f, 1.0f,
            -0.05f, -0.05f,   1.0f, 0.0f, 0.0f,
             0.05f,  0.05f,   0.0f, 0.0f, 1.0f,
            -0.05f,  0.05f,   1.0f, 1.0f, 0.0f
        };

        uint quadVAO, quadVBO;

        _gl.GenVertexArrays(1, out quadVAO);
        _gl.GenBuffers(1, out quadVBO);

        _gl.BindVertexArray(quadVAO);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, quadVBO);
        fixed (float* buf = quadVertices)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(quadVertices.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
        }

        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)0);

        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)(2 * sizeof(float)));

        // define também os dados da instância
        _gl.EnableVertexAttribArray(2);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, instanceVBO); // este atributo vem de um buffer de vértices diferente
        _gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), (void*)0);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _gl.VertexAttribDivisor(2, 1); // informe ao OpenGL que este é um atributo de vértice instanciado.

        // loop de renderização
        // --------------------------------------------------
        while (!_glfw.WindowShouldClose(window))
        {
            // render
            // --------------------------------------------------
            _gl.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // desenha 100 quads instanciados
            shader.Use();
            _gl.BindVertexArray(quadVAO);
            _gl.DrawArraysInstanced(PrimitiveType.Triangles, 0, 6, 100); // 100 triângulos de 6 vértices cada
            _gl.BindVertexArray(0);

            // glfw: troca os buffers e processa eventos de E/S (teclas pressionadas/liberadas, movimento do mouse, etc.)
            // --------------------------------------------------
            _glfw.SwapBuffers(window);
            _glfw.PollEvents();
        }

        // opcional: desalocar todos os recursos assim que não forem mais necessários:
        // --------------------------------------------------
        _gl.DeleteVertexArrays(1, ref quadVAO);
        _gl.DeleteBuffers(1, ref quadVBO);

        // glfw: encerra, liberando todos os recursos do GLFW alocados anteriormente.
        // --------------------------------------------------
        _glfw.Terminate();
    }

    // glfw: sempre que o tamanho da janela é alterado (pelo SO ou por redimensionamento do usuário), esta função de callback é executada
    // --------------------------------------------------
    private static unsafe void FramebufferSizeCallback(WindowHandle* window, int width, int height)
    {
        // certifique-se de que a viewport corresponda às novas dimensões da janela; observe que a largura e
        // a altura serão significativamente maiores do que as especificadas em telas Retina.
        _gl.Viewport(0, 0, (uint)width, (uint)height);
    }
}
