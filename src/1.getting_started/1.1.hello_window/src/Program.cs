using Silk.NET.GLFW;
using Silk.NET.OpenGL;

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

        int pWidth;
        int pHeight;

        // Obtém o tamanho da janela passado para glfwCreateWindow
        _glfw.GetWindowSize(window, out pWidth, out pHeight);

        // Obtém a resolução do monitor principal
        var vidmode = _glfw.GetVideoMode(_glfw.GetPrimaryMonitor());

        // Centralizar a janela
        _glfw.SetWindowPos(
            window,
            (vidmode->Width - pWidth) / 2,
            (vidmode->Height - pHeight) / 2
        );

        _glfw.MakeContextCurrent(window);
        _glfw.SetFramebufferSizeCallback(window, FramebufferSizeCallback);

        // loop de renderização
        // --------------------------------------------------
        while (!_glfw.WindowShouldClose(window))
        {
            // input
            // --------------------------------------------------
            ProcessInput(window);

            // glfw: troca os buffers e processa eventos de E/S (teclas pressionadas/liberadas, movimento do mouse, etc.)
            // --------------------------------------------------
            _glfw.SwapBuffers(window);
            _glfw.PollEvents();
        }

        // glfw: encerra, liberando todos os recursos do GLFW alocados anteriormente.
        // --------------------------------------------------
        _glfw.Terminate();
    }

    // process all input: query GLFW whether relevant keys are pressed/released this frame and react accordingly
    // --------------------------------------------------
    private static unsafe void ProcessInput(WindowHandle* window)
    {
        if (_glfw.GetKey(window, Keys.Escape) == (int)InputAction.Press)
        {
            _glfw.SetWindowShouldClose(window, true);
        }
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
