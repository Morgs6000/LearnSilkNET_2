using System.Numerics;
using System.Runtime.CompilerServices;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;
using StbImageSharp;

namespace LearnSilkNET.src;

public class Program
{
    private static Glfw _glfw = null!;
    private static GL _gl = null!;

    // configurações
    private const int SCR_WIDTH = 800;
    private const int SCR_HEIGHT = 600;

    private GLEnum glCheckError_([CallerFilePath]string file = "", [CallerLineNumber]int line = 0)
    {
        GLEnum errorCode;

        while ((errorCode = _gl.GetError()) != GLEnum.NoError)
        {
            string error = string.Empty;

            switch (errorCode)
            {
                case GLEnum.InvalidEnum:
                    error = "INVALID_ENUM";
                    break;
                case GLEnum.InvalidValue:
                    error = "INVALID_VALUE";
                    break;
                case GLEnum.InvalidOperation:
                    error = "INVALID_OPERATION";
                    break;
                case GLEnum.StackOverflow:
                    error = "STACK_OVERFLOW";
                    break;
                case GLEnum.StackUnderflow:
                    error = "STACK_UNDERFLOW";
                    break;
                case GLEnum.OutOfMemory:
                    error = "OUT_OF_MEMORY";
                    break;
                case GLEnum.InvalidFramebufferOperation:
                    error = "INVALID_FRAMEBUFFER_OPERATION";
                    break;
            }

            Console.WriteLine(error + " | " + file + " (" + line + ")");
        }

        return errorCode;
    }

    private static void glDebugOutput(
        GLEnum source,
        GLEnum type,
        int id,
        GLEnum
        severity,
        int length,
        nint message,
        nint userParam
    )
    {
        if (id == 131169 || id == 131185 || id == 131218 || id == 131204) // ignore estes códigos de erro não significativos
        {
            return;
        }

        Console.WriteLine("---------------");
        Console.WriteLine("Debug message (" + id + "): " + message);

        switch (source)
        {
            case GLEnum.DebugSourceApi:
                Console.Write("Source: API");
                break;
            case GLEnum.DebugSourceWindowSystem:
                Console.Write("Source: Window System");
                break;
            case GLEnum.DebugSourceShaderCompiler:
                Console.Write("Source: Shader Compiler");
                break;
            case GLEnum.DebugSourceThirdParty:
                Console.Write("Source: Third Party");
                break;
            case GLEnum.DebugSourceApplication:
                Console.Write("Source: Application");
                break;
            case GLEnum.DebugSourceOther:
                Console.Write("Source: Other");
                break;
        }
        Console.WriteLine();

        switch (type)
        {
            case GLEnum.DebugTypeError:
                Console.Write("Type: Error");
                break;
            case GLEnum.DebugTypeDeprecatedBehavior:
                Console.Write("Type: Deprecated Behaviour");
                break;
            case GLEnum.DebugTypeUndefinedBehavior:
                Console.Write("Type: Undefined Behaviour");
                break;
            case GLEnum.DebugTypePortability:
                Console.Write("Type: Portability");
                break;
            case GLEnum.DebugTypePerformance:
                Console.Write("Type: Performance");
                break;
            case GLEnum.DebugTypeMarker:
                Console.Write("Type: Marker");
                break;
            case GLEnum.DebugTypePushGroup:
                Console.Write("Type: Push Group");
                break;
            case GLEnum.DebugTypePopGroup:
                Console.Write("Type: Pop Group");
                break;
            case GLEnum.DebugTypeOther:
                Console.Write("Type: Other");
                break;
        }
        Console.WriteLine();

        switch (severity)
        {
            case GLEnum.DebugSeverityHigh:
                Console.Write("Severity: high");
                break;
            case GLEnum.DebugSeverityMedium:
                Console.Write("Severity: medium");
                break;
            case GLEnum.DebugSeverityLow:
                Console.Write("Severity: low");
                break;
            case GLEnum.DebugSeverityNotification:
                Console.Write("Severity: notification");
                break;
        }
        Console.WriteLine();

        Console.WriteLine();
    }

    private static unsafe void Main(string[] args)
    {
        _glfw = Glfw.GetApi();
        _gl = GL.GetApi(_glfw.GetProcAddress);

        // glfw: inicializar e configurar
        // --------------------------------------------------
        _glfw.Init();
        _glfw.WindowHint(WindowHintInt.ContextVersionMajor, 3);
        _glfw.WindowHint(WindowHintInt.ContextVersionMinor, 3);
        _glfw.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);
        _glfw.WindowHint(WindowHintBool.OpenGLDebugContext, true); // comente esta linha em uma build de produção!

        if (OperatingSystem.IsMacOS())
        {
            _glfw.WindowHint(WindowHintBool.OpenGLForwardCompat, true);
        }

        // criação da janela glfw
        // --------------------------------------------------
        WindowHandle* window = _glfw.CreateWindow(SCR_WIDTH, SCR_HEIGHT, "Learn Silk.NET", null, null);

        if (window == null)
        {
            Console.WriteLine("Falha ao criar a janela GLFW");
            _glfw.Terminate();
        }

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
        _glfw.SetFramebufferSizeCallback(window, FramebufferSizeCallback);

        // instruir o GLFW a capturar o mouse
        _glfw.SetInputMode(window, CursorStateAttribute.Cursor, CursorModeValue.CursorDisabled);

        // habilita o contexto de depuração do OpenGL se o contexto permitir um contexto de depuração
        int flags;
        _gl.GetInteger(GetPName.ContextFlags, out flags);

        if ((flags & (int)GLEnum.ContextFlagDebugBit) != 0)
        {
            _gl.Enable(EnableCap.DebugOutput);
            _gl.Enable(EnableCap.DebugOutputSynchronous); // garante que os erros sejam exibidos de forma síncrona
            _gl.DebugMessageCallback(glDebugOutput, null);
            _gl.DebugMessageControl(DebugSource.DontCare, DebugType.DontCare, DebugSeverity.DontCare, 0, null, true);
        }

        // configurar estado global do OpenGL
        // --------------------------------------------------
        _gl.Enable(EnableCap.DepthTest);
        _gl.Enable(EnableCap.CullFace);

        // Estado inicial do OpenGL
        Shader shader = new Shader(_gl, "src/debugging.vs", "src/debugging.fs");

        // configurar cubo 3D
        uint cubeVAO, cubeVBO;

        float[] vertices =
        {
            // positions           // texture coords

            // face esquerda
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f, -0.5f,  0.5f,   1.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f, -0.5f,   0.0f, 1.0f,
            
            // face direita
             0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f, -0.5f, -0.5f,   1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f,  0.5f,  0.5f,   0.0f, 1.0f,
            
            // face inferior
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
             0.5f, -0.5f, -0.5f,   1.0f, 0.0f,
             0.5f, -0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
             0.5f, -0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,   0.0f, 1.0f,
            
            // face superior
            -0.5f,  0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f,  0.5f,   1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f, -0.5f,   0.0f, 1.0f,
            
            // face posterior
             0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f, -0.5f, -0.5f,   1.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f,  0.5f, -0.5f,   0.0f, 1.0f,
            
            // face frontal
            -0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f, -0.5f,  0.5f,   1.0f, 0.0f,
             0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f,  0.5f,   0.0f, 1.0f
        };

        _gl.GenVertexArrays(1, out cubeVAO);
        _gl.GenBuffers(1, out cubeVBO);

        // preencher buffer
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, cubeVBO);
        fixed (float* buf = vertices)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(vertices.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
        }

        // vincular atributos de vértice
        _gl.BindVertexArray(cubeVAO);

        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)0);

        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)(3 * sizeof(float)));

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _gl.BindVertexArray(0);

        // carregar textura de cubo
        uint texture;

        _gl.GenTextures(1, out texture);
        _gl.BindTexture(TextureTarget.Texture2D, texture);

        int width, height;
        byte[] data;

        using (FileStream stream = File.OpenRead("res/textures/wood.png"))
        {
            ImageResult image = ImageResult.FromStream(stream, ColorComponents.Default);

            width = image.Width;
            height = image.Height;
            data = image.Data;
        }

        if (data != null)
        {
            fixed (byte* ptr = data)
            {
                _gl.TexImage2D(GLEnum.Framebuffer, 0, InternalFormat.Rgb, (uint)width, (uint)height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, ptr);
            }
            _gl.GenerateMipmap(TextureTarget.Texture2D);

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        }
        else
        {
            Console.WriteLine("Falha ao carregar a textura");
        }

        // configurar a matriz de projeção
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
            fieldOfView:       MathHelper.DegreesToRadians(45.0f), 
            aspectRatio:       (float)SCR_WIDTH / (float)SCR_HEIGHT, 
            nearPlaneDistance: 0.1f, 
            farPlaneDistance:  10.0f
        );

        // loop de renderização
        // --------------------------------------------------
        while (!_glfw.WindowShouldClose(window))
        {
            // input
            // --------------------------------------------------
            ProcessInput(window);

            // render
            // --------------------------------------------------
            _gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            shader.Use();

            float rotationSpeed = 10.0f;
            float angle = (float)_glfw.GetTime() * rotationSpeed;

            Matrix4x4 model = Matrix4x4.Identity;
            model *= Matrix4x4.CreateFromAxisAngle(Vector3.Normalize(new Vector3(1.0f, 1.0f, 1.0f)), MathHelper.DegreesToRadians(angle));
            model *= Matrix4x4.CreateTranslation(new Vector3(0.0f, 0.0f, -2.5f));
            shader.SetMat4("model", model);

            _gl.BindTexture(TextureTarget.Texture2D, texture);
            _gl.BindVertexArray(cubeVAO);
                _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
            _gl.BindVertexArray(0);

            // glfw: troca os buffers e processa eventos de E/S (teclas pressionadas/liberadas, movimento do mouse, etc.)
            // --------------------------------------------------
            _glfw.SwapBuffers(window);
            _glfw.PollEvents();
        }

        _glfw.Terminate();
    }

    // renderQuad() renderiza um quadrilátero XY de 1x1 em NDC
    // --------------------------------------------------
    private static uint _quadVAO = 0;
    private static uint _quadVBO;

    private static unsafe void RenderQuad()
    {
        if (_quadVAO == 0)
        {
            float[] quadVertices =
            {
                // positions           // texture Coords
                -1.0f,  1.0f,  0.0f,   0.0f, 1.0f,
                -1.0f, -1.0f,  0.0f,   0.0f, 0.0f,
                 1.0f,  1.0f,  0.0f,   1.0f, 1.0f,
                 1.0f, -1.0f,  0.0f,   1.0f, 0.0f
            };

            // setup plane VAO
            _gl.GenVertexArrays(1, out _quadVAO);
            _gl.GenBuffers(1, out _quadVBO);

            _gl.BindVertexArray(_quadVAO);

            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _quadVBO);
            fixed (float* buf = quadVertices)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(quadVertices.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
            }

            _gl.EnableVertexAttribArray(0);
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)0);

            _gl.EnableVertexAttribArray(1);
            _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)(3 * sizeof(float)));
        }

        _gl.BindVertexArray(_quadVAO);
        _gl.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        _gl.BindVertexArray(0);
    }

    // processar toda a entrada: consultar a GLFW para saber se teclas relevantes foram pressionadas ou liberadas neste quadro e reagir de acordo
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
