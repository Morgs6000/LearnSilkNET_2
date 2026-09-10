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
        Shader shader = new Shader(_gl, "src/cubemaps.vs", "src/cubemaps.fs");
        Shader skyboxShader = new Shader(_gl, "src/skybox.vs", "src/skybox.fs");
        
        // configurar dados de vértice (e buffer(s)) e configurar atributos de vértice
        // --------------------------------------------------
        float[] cubeVertices =
        {
            // positions           // texture Coords
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f, -0.5f,  0.5f,   1.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f, -0.5f,   0.0f, 1.0f,
            
             0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f, -0.5f, -0.5f,   1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f,  0.5f,  0.5f,   0.0f, 1.0f,
            
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
             0.5f, -0.5f, -0.5f,   1.0f, 0.0f,
             0.5f, -0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
             0.5f, -0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,   0.0f, 1.0f,
            
            -0.5f,  0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f,  0.5f,   1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f, -0.5f,   0.0f, 1.0f,
            
             0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f, -0.5f, -0.5f,   1.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f,  0.5f, -0.5f,   0.0f, 1.0f,
            
            -0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f, -0.5f,  0.5f,   1.0f, 0.0f,
             0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f,  0.5f,   0.0f, 1.0f
        };

        float[] skyboxVertices =
        {
            // positions  
            -1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f,  1.0f,
            -1.0f,  1.0f,  1.0f,
            -1.0f, -1.0f, -1.0f,
            -1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f, -1.0f,
            
             1.0f, -1.0f,  1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f,  1.0f, -1.0f,
             1.0f, -1.0f,  1.0f,
             1.0f,  1.0f, -1.0f,
             1.0f,  1.0f,  1.0f,
            
            -1.0f, -1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f, -1.0f,  1.0f,
            -1.0f, -1.0f, -1.0f,
             1.0f, -1.0f,  1.0f,
            -1.0f, -1.0f,  1.0f,
            
            -1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f,  1.0f,
             1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
            
             1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
             1.0f,  1.0f, -1.0f,
            
            -1.0f, -1.0f,  1.0f,
             1.0f, -1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
            -1.0f, -1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f,  1.0f
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
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)0);

        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)(3 * sizeof(float)));

        // skybox VAO
        uint skyboxVAO, skyboxVBO;

        _gl.GenVertexArrays(1, out skyboxVAO);
        _gl.GenBuffers(1, out skyboxVBO);

        _gl.BindVertexArray(skyboxVAO);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, skyboxVBO);
        fixed (float* buf = skyboxVertices)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(skyboxVertices.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
        }

        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);

        // carregar texturas
        // --------------------------------------------------
        uint cubeTexture = LoadTexture("res/textures/container.jpg");

        List<string> faces = new()
        {
            "res/textures/skybox/right.jpg",
            "res/textures/skybox/left.jpg",
            "res/textures/skybox/top.jpg",
            "res/textures/skybox/bottom.jpg",
            "res/textures/skybox/front.jpg",
            "res/textures/skybox/back.jpg"
        };

        uint cubemapTexture = LoadCubmap(faces);

        // configuração do shader
        // --------------------------------------------------
        shader.Use();
        shader.SetInt("texture1", 0);

        skyboxShader.Use();
        skyboxShader.SetInt("skybox", 0);

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

            // desenha a cena normalmente
            shader.Use();
            
            Matrix4x4 model = Matrix4x4.Identity;
            Matrix4x4 view = _camera.GetViewMatrix();
            Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
                fieldOfView:       MathHelper.DegreesToRadians(_camera.Zoom), 
                aspectRatio:       (float)SCR_WIDTH / (float)SCR_HEIGHT, 
                nearPlaneDistance: 0.1f, 
                farPlaneDistance:  100.0f
            );
            
            shader.SetMat4("model", model);
            shader.SetMat4("view", view);
            shader.SetMat4("projection", projection);

            // cubos
            _gl.BindVertexArray(cubeVAO);

            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, cubeTexture);

            _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
            _gl.BindVertexArray(0);
            
            // desenhar o skybox por último
            _gl.DepthFunc(DepthFunction.Lequal); // altera a função de profundidade para que o teste de profundidade passe quando os valores forem iguais ao conteúdo do buffer de profundidade

            skyboxShader.Use();

            Matrix4x4 m = _camera.GetViewMatrix(); // remove a translação da matriz de visualização
            view = new Matrix4x4(
                m.M11, m.M12, m.M13, 0.0f,
                m.M21, m.M22, m.M23, 0.0f,
                m.M31, m.M32, m.M33, 0.0f,
                0.0f,  0.0f,  0.0f,  1.0f
            );

            skyboxShader.SetMat4("view", view);
            skyboxShader.SetMat4("projection", projection);

            // cubo de skybox
            _gl.BindVertexArray(skyboxVAO);

            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.TextureCubeMap, cubemapTexture);

            _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);

            _gl.BindVertexArray(0);
            _gl.DepthFunc(DepthFunction.Less); // redefine a função de profundidade para o padrão

            // glfw: troca os buffers e processa eventos de E/S (teclas pressionadas/liberadas, movimento do mouse, etc.)
            // --------------------------------------------------
            _glfw.SwapBuffers(window);
            _glfw.PollEvents();
        }

        // opcional: desalocar todos os recursos assim que não forem mais necessários:
        // --------------------------------------------------
        _gl.DeleteVertexArrays(1, ref cubeVAO);
        _gl.DeleteVertexArrays(1, ref skyboxVAO);
        _gl.DeleteBuffers(1, ref cubeVBO);
        _gl.DeleteBuffers(1, ref skyboxVBO);

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

    // função utilitária para carregar uma textura 2D a partir de um arquivo
    // --------------------------------------------------
    private static unsafe uint LoadTexture(string path)
    {
        uint textureID;
        _gl.GenTextures(1, out textureID);

        int width, height;
        byte[] data;

        ImageResult image;

        using (FileStream stream = File.OpenRead(path))
        {
            image = ImageResult.FromStream(stream, ColorComponents.Default);

            width = image.Width;
            height = image.Height;
            data = image.Data;
        }

        if (data != null)
        {
            InternalFormat internalFormat = InternalFormat.Rgb;
            PixelFormat pixelFormat = PixelFormat.Rgb;

            if (image.Comp == ColorComponents.Grey)
            {
                internalFormat = InternalFormat.Red;
                pixelFormat = PixelFormat.Red;
            }
            else if (image.Comp == ColorComponents.RedGreenBlue)
            {
                internalFormat = InternalFormat.Rgb;
                pixelFormat = PixelFormat.Rgb;
            }
            else if (image.Comp == ColorComponents.RedGreenBlueAlpha)
            {
                internalFormat = InternalFormat.Rgba;
                pixelFormat = PixelFormat.Rgba;
            }

            _gl.BindTexture(TextureTarget.Texture2D, textureID);
            fixed (byte* ptr = data)
            {
                _gl.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, (uint)width, (uint)height, 0, pixelFormat, PixelType.UnsignedByte, ptr);
            }
            _gl.GenerateMipmap(TextureTarget.Texture2D);

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        }
        else
        {
            Console.WriteLine("Falha ao carregar a textura no caminho: " + path);
        }

        return textureID;
    }

    // carrega uma textura cubemap a partir de 6 faces de textura individuais
    // ordem:
    // +X (direita)
    // -X (esquerda)
    // +Y (topo)
    // -Y (base)
    // +Z (frente)
    // -Z (trás)
    // --------------------------------------------------
    private static unsafe uint LoadCubmap(List<string> faces)
    {
        uint textureID;

        _gl.GenTextures(1, out textureID);
        _gl.BindTexture(TextureTarget.TextureCubeMap, textureID);

        int width, height;
        byte[] data;

        for (int i = 0; i < faces.Count(); i++)
        {
            using (FileStream stream = File.OpenRead(faces[i]))
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
                    _gl.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, InternalFormat.Rgb, (uint)width, (uint)height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, ptr);
                }
            }
            else
            {
                Console.WriteLine("A textura de cubemap falhou ao carregar no caminho: " + faces[i]);
            }
        }

        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);

        return textureID;
    }
}
