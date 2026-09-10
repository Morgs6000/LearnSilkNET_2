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
        Shader shaderRed = new Shader(_gl, "src/advanced_glsl.vs", "src/red.fs");
        Shader shaderGreen = new Shader(_gl, "src/advanced_glsl.vs", "src/green.fs");
        Shader shaderBlue = new Shader(_gl, "src/advanced_glsl.vs", "src/blue.fs");
        Shader shaderYellow = new Shader(_gl, "src/advanced_glsl.vs", "src/yellow.fs");
        
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

        // cube VAO
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

        // configurar um objeto de buffer uniforme
        // --------------------------------------------------

        // primeiro. Obtemos os índices de bloco relevantes
        uint uniformBlockIndexRed = _gl.GetUniformBlockIndex(shaderRed.ID, "Matrices");
        uint uniformBlockIndexGreen = _gl.GetUniformBlockIndex(shaderGreen.ID, "Matrices");
        uint uniformBlockIndexBlue = _gl.GetUniformBlockIndex(shaderBlue.ID, "Matrices");
        uint uniformBlockIndexYellow = _gl.GetUniformBlockIndex(shaderYellow.ID, "Matrices");

        // então, vinculamos o bloco de uniformes de cada shader a este ponto de vinculação de uniformes
        _gl.UniformBlockBinding(shaderRed.ID, uniformBlockIndexRed, 0);
        _gl.UniformBlockBinding(shaderGreen.ID, uniformBlockIndexGreen, 0);
        _gl.UniformBlockBinding(shaderBlue.ID, uniformBlockIndexBlue, 0);
        _gl.UniformBlockBinding(shaderYellow.ID, uniformBlockIndexYellow, 0);

        // Agora, de fato, crie o buffer
        uint uboMatrices;

        _gl.GenBuffers(1, out uboMatrices);
        _gl.BindBuffer(BufferTargetARB.UniformBuffer, uboMatrices);
        _gl.BufferData(BufferTargetARB.UniformBuffer, (uint)(2 * sizeof(Matrix4x4)), null, BufferUsageARB.StaticDraw);
        _gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);

        // define o intervalo do buffer que se conecta a um ponto de vinculação de uniform
        _gl.BindBufferRange(BufferTargetARB.UniformBuffer, 0, uboMatrices, 0, (uint)(2 * sizeof(Matrix4x4)));

        // armazena a matriz de projeção (agora fazemos isso apenas uma vez) (nota: não usamos mais o zoom alterando o FoV)
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
            fieldOfView:       MathHelper.DegreesToRadians(45.0f), 
            aspectRatio:       (float)SCR_WIDTH / (float)SCR_HEIGHT, 
            nearPlaneDistance: 0.1f, 
            farPlaneDistance:  100.0f
        );

        _gl.BindBuffer(BufferTargetARB.UniformBuffer, uboMatrices);
        _gl.BufferSubData(BufferTargetARB.UniformBuffer, 0, (uint)(sizeof(Matrix4x4)), (float*)&projection);
        _gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);

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

            // define as matrizes de visualização e projeção no bloco uniform — só precisamos fazer isso uma vez por iteração do loop.
            Matrix4x4 view = _camera.GetViewMatrix();

            _gl.BindBuffer(BufferTargetARB.UniformBuffer, uboMatrices);
            _gl.BufferSubData(BufferTargetARB.UniformBuffer, sizeof(Matrix4x4), (uint)(sizeof(Matrix4x4)), (float*)&view);
            _gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);

            // desenhar 4 cubos

            // VERMELHO
            _gl.BindVertexArray(cubeVAO);
            shaderRed.Use();
            Matrix4x4 model = Matrix4x4.Identity;
            model *= Matrix4x4.CreateTranslation(new Vector3(-0.75f, 0.75f, 0.0f)); // mover para o canto superior esquerdo
            shaderRed.SetMat4("model", model);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);

            // VERDE
            shaderGreen.Use();
            model = Matrix4x4.Identity;
            model *= Matrix4x4.CreateTranslation(new Vector3(0.75f, 0.75f, 0.0f)); // mover para o canto superior direito
            shaderGreen.SetMat4("model", model);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);

            // AMARELO
            shaderYellow.Use();
            model = Matrix4x4.Identity;
            model *= Matrix4x4.CreateTranslation(new Vector3(-0.75f, -0.75f, 0.0f)); // mover para baixo à esquerda
            shaderYellow.SetMat4("model", model);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);

            // AZUL
            shaderBlue.Use();
            model = Matrix4x4.Identity;
            model *= Matrix4x4.CreateTranslation(new Vector3(0.75f, -0.75f, 0.0f)); // mover para baixo e para a direita
            shaderBlue.SetMat4("model", model);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);

            // glfw: troca os buffers e processa eventos de E/S (teclas pressionadas/liberadas, movimento do mouse, etc.)
            // --------------------------------------------------
            _glfw.SwapBuffers(window);
            _glfw.PollEvents();
        }

        // opcional: desalocar todos os recursos assim que não forem mais necessários:
        // --------------------------------------------------
        _gl.DeleteVertexArrays(1, ref cubeVAO);
        _gl.DeleteBuffers(1, ref cubeVBO);

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
