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

    // câmera
    private static Camera _camera = new Camera(new Vector3(0.0f, 0.0f, 3.0f));
    private static float _lastX = SCR_WIDTH / 2.0f;
    private static float _lastY = SCR_HEIGHT / 2.0f;
    private static bool _firstMouse = true;

    // tempo
    private static float _deltaTime = 0.0f;
    private static float _lastFrame = 0.0f;

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
        _glfw.SetFramebufferSizeCallback(window, FramebufferSizeCallback);
        _glfw.SetCursorPosCallback(window, MouseCallback);
        _glfw.SetScrollCallback(window, ScrollCallback);

        // instruir o GLFW a capturar o mouse
        _glfw.SetInputMode(window, CursorStateAttribute.Cursor, CursorModeValue.CursorDisabled);

        // configurar estado global do OpenGL
        // --------------------------------------------------
        _gl.Enable(EnableCap.DepthTest);

        // construir e compilar nosso programa de shader
        // --------------------------------------------------
        Shader shader = new Shader(_gl, "src/anti_aliasing.vs", "src/anti_aliasing.fs");
        Shader screenShader = new Shader(_gl, "src/aa_post.vs", "src/aa_post.fs");
        
        // configurar dados de vértice (e buffer(s)) e configurar atributos de vértice
        // --------------------------------------------------
        float[] cubeVertices =
        {
            // positions  
            -0.5f, -0.5f, -0.5f,
            -0.5f, -0.5f,  0.5f,
            -0.5f,  0.5f,  0.5f,
            -0.5f, -0.5f, -0.5f,
            -0.5f,  0.5f,  0.5f,
            -0.5f,  0.5f, -0.5f,
            
             0.5f, -0.5f,  0.5f,
             0.5f, -0.5f, -0.5f,
             0.5f,  0.5f, -0.5f,
             0.5f, -0.5f,  0.5f,
             0.5f,  0.5f, -0.5f,
             0.5f,  0.5f,  0.5f,
            
            -0.5f, -0.5f, -0.5f,
             0.5f, -0.5f, -0.5f,
             0.5f, -0.5f,  0.5f,
            -0.5f, -0.5f, -0.5f,
             0.5f, -0.5f,  0.5f,
            -0.5f, -0.5f,  0.5f,
            
            -0.5f,  0.5f,  0.5f,
             0.5f,  0.5f,  0.5f,
             0.5f,  0.5f, -0.5f,
            -0.5f,  0.5f,  0.5f,
             0.5f,  0.5f, -0.5f,
            -0.5f,  0.5f, -0.5f,
            
             0.5f, -0.5f, -0.5f,
            -0.5f, -0.5f, -0.5f,
            -0.5f,  0.5f, -0.5f,
             0.5f, -0.5f, -0.5f,
            -0.5f,  0.5f, -0.5f,
             0.5f,  0.5f, -0.5f,
            
            -0.5f, -0.5f,  0.5f,
             0.5f, -0.5f,  0.5f,
             0.5f,  0.5f,  0.5f,
            -0.5f, -0.5f,  0.5f,
             0.5f,  0.5f,  0.5f,
            -0.5f,  0.5f,  0.5f
        };

        float[] quadVertices = // atributos de vértice para um quadrilátero que preenche a tela inteira em Coordenadas de Dispositivo Normalizadas.
        {
            // positions    // texCoords
            -1.0f, -1.0f,   0.0f, 0.0f,
             1.0f, -1.0f,   1.0f, 0.0f,
             1.0f,  1.0f,   1.0f, 1.0f,
            -1.0f, -1.0f,   0.0f, 0.0f,
             1.0f,  1.0f,   1.0f, 1.0f,
            -1.0f,  1.0f,   0.0f, 1.0f
        };

        // setup cube VAO
        uint cubeVAO, cubeVBO;

        _gl.GenVertexArrays(1, out cubeVAO);
        _gl.GenBuffers(1, out cubeVBO);

        _gl.BindVertexArray(cubeVAO);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, cubeVBO);
        fixed (float* buf = cubeVertices)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(cubeVertices.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
        }

        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);

        // setup screen VAO
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
        _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)0);

        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));

        // configurar framebuffer MSAA
        // --------------------------------------------------
        uint framebuffer;

        _gl.GenFramebuffers(1, out framebuffer);
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);

        // criar uma textura de anexo de cor com multisampling
        uint textureColorBufferMultiSampled;

        _gl.GenTextures(1, out textureColorBufferMultiSampled);
        _gl.BindTexture(TextureTarget.Texture2DMultisample, textureColorBufferMultiSampled);

        _gl.TexImage2DMultisample(TextureTarget.Texture2DMultisample, 4, InternalFormat.Rgb, SCR_WIDTH, SCR_HEIGHT, true);

        _gl.BindTexture(TextureTarget.Texture2DMultisample, 0);
        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2DMultisample, textureColorBufferMultiSampled, 0);

        // cria um objeto renderbuffer (também com multisampling) para anexos de profundidade e stencil
        uint rbo;

        _gl.GenRenderbuffers(1, out rbo);
        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo);

        _gl.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, 4, InternalFormat.Depth24Stencil8, SCR_WIDTH, SCR_HEIGHT);

        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
        _gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, rbo);

        if (_gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
        {
            Console.WriteLine("ERROR::FRAMEBUFFER:: Framebuffer is not complete!");
        }

        // configurar o segundo framebuffer de pós-processamento
        uint intermediateFBO;

        _gl.GenFramebuffers(1, out intermediateFBO);
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, intermediateFBO);

        // criar uma textura de anexo de cor
        uint screenTexture;

        _gl.GenTextures(1, out screenTexture);
        _gl.BindTexture(TextureTarget.Texture2D, screenTexture);

        _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb, SCR_WIDTH, SCR_HEIGHT, 0, PixelFormat.Rgb, PixelType.UnsignedByte, null);

        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, screenTexture, 0); // precisamos apenas de um buffer de cor

        if (_gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
        {
            Console.WriteLine("ERROR::FRAMEBUFFER:: Intermediate framebuffer is not complete!");
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // configuração do shader
        // --------------------------------------------------
        screenShader.Use();
        screenShader.SetInt("screenTexture", 0);

        // loop de renderização
        // --------------------------------------------------
        while (!_glfw.WindowShouldClose(window))
        {
            // lógica de tempo por quadro
            // --------------------------------------------------
            float currentFrame = (float)_glfw.GetTime();
            _deltaTime = currentFrame - _lastFrame;
            _lastFrame = currentFrame;

            // input
            // --------------------------------------------------
            ProcessInput(window);

            // render
            // --------------------------------------------------
            _gl.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // 1. desenha a cena normalmente em buffers com multisampling
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
            _gl.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            _gl.Enable(EnableCap.DepthTest);

            // definir matrizes de transformação
            shader.Use();

            Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
                fieldOfView:       MathHelper.DegreesToRadians(45.0f), 
                aspectRatio:       (float)SCR_WIDTH / (float)SCR_HEIGHT, 
                nearPlaneDistance: 0.1f, 
                farPlaneDistance:  1000.0f
            );

            shader.SetMat4("projection", projection);
            shader.SetMat4("view", _camera.GetViewMatrix());
            shader.SetMat4("model", Matrix4x4.Identity);

            _gl.BindVertexArray(cubeVAO);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);

            // 2. agora, transfira o(s) buffer(s) com multisampling para o buffer de cor normal do FBO intermediário. A imagem é armazenada em screenTexture.
            _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, framebuffer);
            _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, intermediateFBO);
            _gl.BlitFramebuffer(0, 0, (int)SCR_WIDTH, (int)SCR_HEIGHT, 0, 0, (int)SCR_WIDTH, (int)SCR_HEIGHT, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);

            // 3. agora, renderize o quadrilátero usando os elementos visuais da cena como sua textura
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            _gl.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
            _gl.Clear(ClearBufferMask.ColorBufferBit);
            _gl.Disable(EnableCap.DepthTest);

            // desenhar quad da tela
            screenShader.Use();
            _gl.BindVertexArray(quadVAO);
            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, screenTexture); // use o anexo de cor já resolvido como a textura do quad
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

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

        if (_glfw.GetKey(window, Keys.W) == (int)InputAction.Press)
        {
            _camera.ProcessKeyboard(CameraMovement.FORWARD, _deltaTime);
        }
        if (_glfw.GetKey(window, Keys.S) == (int)InputAction.Press)
        {
            _camera.ProcessKeyboard(CameraMovement.BACKWARD, _deltaTime);
        }
        if (_glfw.GetKey(window, Keys.A) == (int)InputAction.Press)
        {
            _camera.ProcessKeyboard(CameraMovement.LEFT, _deltaTime);
        }
        if (_glfw.GetKey(window, Keys.D) == (int)InputAction.Press)
        {
            _camera.ProcessKeyboard(CameraMovement.RIGHT, _deltaTime);
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

    // glfw: sempre que o mouse se move, este callback é chamado
    // --------------------------------------------------
    private static unsafe void MouseCallback(WindowHandle* window, double xposIn, double yposIn)
    {
        float xpos = (float)xposIn;
        float ypos = (float)yposIn;

        if (_firstMouse)
        {
            _lastX = xpos;
            _lastY = ypos;

            _firstMouse = false;
        }

        float xoffset = xpos - _lastX;
        float yoffset = _lastY - ypos; // invertido, já que as coordenadas y vão de baixo para cima

        _lastX = xpos;
        _lastY = ypos;

        _camera.ProcessMouseMovement(xoffset, yoffset);
    }

    // glfw: sempre que a roda de rolagem do mouse é girada, este callback é chamado
    // --------------------------------------------------
    private static unsafe void ScrollCallback(WindowHandle* window, double xoffset, double yoffset)
    {
        _camera.ProcessMouseScroll((float)yoffset);
    }
}
