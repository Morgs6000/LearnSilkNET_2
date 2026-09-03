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

        // construir e compilar nosso programa de shader
        // --------------------------------------------------
        Shader ourShader = new Shader(_gl, "src/texture.vs", "src/texture.fs");

        // configurar dados de vértice (e buffer(s)) e configurar atributos de vértice
        // --------------------------------------------------
        float[] vertices =
        {
            // positions          // colors           // texture coords
            -0.5f, -0.5f, 0.0f,   1.0f, 0.0f, 0.0f,   0.0f, 0.0f, // inferior esquerdo
             0.5f, -0.5f, 0.0f,   0.0f, 1.0f, 0.0f,   1.0f, 0.0f, // inferior direito
             0.5f,  0.5f, 0.0f,   0.0f, 0.0f, 1.0f,   1.0f, 1.0f, // superior direito
            -0.5f,  0.5f, 0.0f,   1.0f, 0.0f, 0.0f,   0.0f, 1.0f  // superior esquerdo
        };

        uint[] indices =
        {
            0, 1, 2, // primeiro triângulo
            0, 2, 3  // segundo triângulo
        };

        uint VAO, VBO, EBO;

        _gl.GenVertexArrays(1, out VAO);
        _gl.GenBuffers(1, out VBO);
        _gl.GenBuffers(1, out EBO);

        // primeiro vincule o Vertex Array Object, depois vincule e configure o(s) buffer(s) de vértices e, em seguida, configure o(s) atributo(s) de vértice.
        _gl.BindVertexArray(VAO);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, VBO);
        fixed (float* buf = vertices)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(vertices.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
        }

        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, EBO);
        fixed (uint* buf = indices)
        {
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (uint)(indices.Length * sizeof(uint)), buf, BufferUsageARB.StaticDraw);
        }

        // position attribute
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), (void*)0);
        _gl.EnableVertexAttribArray(0);

        // color attribute
        _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), (void*)(3 * sizeof(float)));
        _gl.EnableVertexAttribArray(1);

        // texture coord attribute
        _gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), (void*)(6 * sizeof(float)));
        _gl.EnableVertexAttribArray(2);
        
        // carregar e criar uma textura
        // --------------------------------------------------
        uint texture;

        _gl.GenTextures(1, out texture);
        _gl.BindTexture(TextureTarget.Texture2D, texture); // todas as operações GL_TEXTURE_2D subsequentes agora afetam este objeto de textura

        // define os parâmetros de repetição da textura
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat); // define o modo de repetição da textura como GL_REPEAT (método padrão)
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        // definir parâmetros de filtragem de textura
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        // carregar imagem, criar textura e gerar mipmaps
        int width, height;
        byte[] data;

        using (FileStream stream = File.OpenRead("res/textures/container.jpg"))
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
                _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb, (uint)width, (uint)height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, ptr);
            }
            _gl.GenerateMipmap(TextureTarget.Texture2D);
        }
        else
        {
            Console.WriteLine("Falha ao carregar a textura");
        }

        // loop de renderização
        // --------------------------------------------------
        while (!_glfw.WindowShouldClose(window))
        {
            // input
            // --------------------------------------------------
            ProcessInput(window);

            // render
            // --------------------------------------------------
            _gl.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            _gl.Clear(ClearBufferMask.ColorBufferBit);

            // vincular textura
            _gl.BindTexture(TextureTarget.Texture2D, texture);
                        
            // renderizar contêiner
            ourShader.Use();
            _gl.BindVertexArray(VAO);
            _gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, (void*)0);

            // glfw: troca os buffers e processa eventos de E/S (teclas pressionadas/liberadas, movimento do mouse, etc.)
            // --------------------------------------------------
            _glfw.SwapBuffers(window);
            _glfw.PollEvents();
        }

        // opcional: desalocar todos os recursos assim que não forem mais necessários:
        // --------------------------------------------------
        _gl.DeleteVertexArrays(1, ref VAO);
        _gl.DeleteBuffers(1, ref VBO);
        _gl.DeleteBuffers(1, ref EBO);

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
