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
    private static Camera _camera = new Camera(new Vector3(0.0f, 0.0f, 55.0f));
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
        Shader shader = new Shader(_gl, "src/instancing.vs", "src/instancing.fs");
        
        // carregar modelos
        // --------------------------------------------------
        Model rock = new Model(_gl, "res/objects/rock/rock.obj");
        Model planet = new Model(_gl, "res/objects/planet/planet.obj");

        // gerar uma lista grande de matrizes de transformação de modelo semialeatórias
        // --------------------------------------------------
        uint amount = 1000;
        Matrix4x4[] modelMatrices = new Matrix4x4[amount];

        Random srand = new Random((int)_glfw.GetTime()); // inicializar a semente de números aleatórios

        float radius = 50.0f;
        float offset = 2.5f;

        for (int i = 0; i < amount; i++)
        {
            Matrix4x4 model = Matrix4x4.Identity;

            // 1. rotação: adicionar rotação aleatória em torno de um vetor de eixo de rotação escolhido de forma (semi)aleatória
            float rotAngle = (float)(srand.Next() % 360);
            model *= Matrix4x4.CreateFromAxisAngle(Vector3.Normalize(new Vector3(0.4f, 0.6f, 0.8f)), rotAngle);

            // 2. Escala: Escala entre 0,05 e 0,25f
            float scale = (float)((srand.Next() % 20) / 100.0f + 0.05f);
            model *= Matrix4x4.CreateScale(new Vector3(scale));

            // 3. tradução: deslocar ao longo de um círculo com 'raio' no intervalo [-offset, offset]
            float angle = (float)i / (float)amount * 360.0f;
            float displacement = (srand.Next() % (int)(2 * offset * 100)) / 100.0f - offset;
            float x = MathF.Sin(angle) * radius + displacement;
            displacement = (srand.Next() % (int)(2 * offset * 100)) / 100.0f - offset;
            float y = displacement * 0.4f; // mantenha a altura do campo de asteroides menor em relação à largura nos eixos x e z
            displacement = (srand.Next() % (int)(2 * offset * 100)) / 100.0f - offset;
            float z = MathF.Cos(angle) * radius + displacement;
            model *= Matrix4x4.CreateTranslation(new Vector3(x, y, z));

            // 4. agora adicione à lista de matrizes
            modelMatrices[i] = model;
        }

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

            // configurar matrizes de transformação
            Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
                fieldOfView:       MathHelper.DegreesToRadians(45.0f), 
                aspectRatio:       (float)SCR_WIDTH / (float)SCR_HEIGHT, 
                nearPlaneDistance: 0.1f, 
                farPlaneDistance:  1000.0f
            );
            Matrix4x4 view = _camera.GetViewMatrix();

            shader.Use();
            shader.SetMat4("projection", projection);
            shader.SetMat4("view", view);

            // desenhar planeta
            Matrix4x4 model = Matrix4x4.Identity;
            model *= Matrix4x4.CreateScale(new Vector3(4.0f, 4.0f, 4.0f));
            model *= Matrix4x4.CreateTranslation(new Vector3(0.0f, -3.0f, 0.0f));
            shader.SetMat4("model", model);
            planet.Draw(shader);

            // desenhar meteoritos
            for (int i = 0; i < amount; i++)
            {
                shader.SetMat4("model", modelMatrices[i]);
                rock.Draw(shader);
            }

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
