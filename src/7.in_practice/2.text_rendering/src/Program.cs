using System.Numerics;
using System.Runtime.InteropServices;
using FreeTypeSharp;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;
using static FreeTypeSharp.FT;
using static FreeTypeSharp.FT_LOAD;

namespace LearnSilkNET.src;

public class Program
{
    private static Glfw _glfw = null!;
    private static GL _gl = null!;

    // configurações
    private const int SCR_WIDTH = 800;
    private const int SCR_HEIGHT = 600;
    
    // Armazena todas as informações de estado relevantes para um caractere, conforme carregado usando o FreeType
    private struct Character
    {
        public uint TextureID;  // Identificador da textura do glifo
        public Vector2 Size;    // Tamanho do glifo
        public Vector2 Bearing; // Deslocamento da linha de base até a esquerda/topo do glifo
        public uint Advance;    // Deslocamento horizontal para avançar para o próximo glifo
    }

    private static Dictionary<uint, Character> Characters = [];
    private static uint VAO, VBO;

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

        // Estado do OpenGL
        // --------------------------------------------------
        _gl.Enable(EnableCap.CullFace);
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // compile and setup the shader
        // --------------------------------------------------
        Shader shader = new Shader(_gl, "src/text.vs", "src/text.fs");

        Matrix4x4 projection = Matrix4x4.CreateOrthographicOffCenter(
            left:        0.0f, 
            right:       (float)SCR_WIDTH, 
            bottom:      0.0f, 
            top:         (float)SCR_HEIGHT, 
            zNearPlane: -1.0f, 
            zFarPlane:   1.0f
        );

        shader.Use();
        shader.SetMat4("projection", projection);

        // FreeType
        // --------------------------------------------------
        FT_LibraryRec_* ft;

        // Todas as funções retornam um valor diferente de 0 sempre que ocorre um erro
        if (FT_Init_FreeType(&ft) != 0)
        {
            Console.WriteLine("ERROR::FREETYPE: Could not init FreeType Library");
        }

        // encontrar caminho para a fonte
        string font_name = "res/fonts/Antonio-Bold.ttf";

        if (font_name == string.Empty)
        {
            Console.WriteLine("ERROR::FREETYPE: Failed to load font_name");
        }

        // carregar fonte como face
        FT_FaceRec_* face;

        if (FT_New_Face(ft, (byte*)Marshal.StringToHGlobalAnsi(font_name), 0, &face) != 0)
        {
            Console.WriteLine("ERROR::FREETYPE: Failed to load font");
        }
        else
        {
            // Defina o tamanho para carregar os glifos como
            FT_Set_Pixel_Sizes(face, 0, 48);

            // desativar a restrição de alinhamento de bytes
            _gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            // carrega os primeiros 128 caracteres do conjunto ASCII
            for (uint c = 0; c < 128; c++)
            {
                // Carregar glifo de caractere
                if (FT_Load_Char(face, c, FT_LOAD_RENDER) != 0)
                {
                    Console.WriteLine("ERROR::FREETYTPE: Failed to load Glyph");
                    continue;
                }

                // gerar textura
                uint texture;

                _gl.GenTextures(1, out texture);
                _gl.BindTexture(TextureTarget.Texture2D, texture);

                _gl.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    InternalFormat.Red,
                    face->glyph->bitmap.width,
                    face->glyph->bitmap.rows,
                    0,
                    PixelFormat.Red,
                    PixelType.UnsignedByte,
                    face->glyph->bitmap.buffer
                );

                // definir opções de textura
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                // agora armazene o caractere para uso posterior
                Character character = new Character()
                {
                    TextureID = texture,
                    Size = new Vector2(face->glyph->bitmap.width, face->glyph->bitmap.rows),
                    Bearing = new Vector2(face->glyph->bitmap_left, face->glyph->bitmap_top),
                    Advance = (uint)face->glyph->advance.x
                };

                Characters.Add(c, character);
            }

            _gl.BindTexture(TextureTarget.Texture2D, 0);
        }

        // destruir o FreeType assim que terminarmos
        FT_Done_Face(face);
        FT_Done_FreeType(ft);

        // configurar VAO/VBO para quads de textura
        // --------------------------------------------------
        _gl.GenVertexArrays(1, out VAO);
        _gl.GenBuffers(1, out VBO);

        _gl.BindVertexArray(VAO);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, VBO);
        _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(sizeof(float) * 6 * 4), null, BufferUsageARB.DynamicDraw);

        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)0);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _gl.BindVertexArray(0);

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

            RenderText(shader, "This is sample text", 25.0f, 25.0f, 1.0f, new Vector3(0.5f, 0.8f, 0.2f));
            RenderText(shader, "(C) LearnOpenGL.com", 540.0f, 570.0f, 0.5f, new Vector3(0.3f, 0.7f, 0.9f));

            // glfw: troca os buffers e processa eventos de E/S (teclas pressionadas/liberadas, movimento do mouse, etc.)
            // --------------------------------------------------
            _glfw.SwapBuffers(window);
            _glfw.PollEvents();
        }

        _glfw.Terminate();
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

    // renderizar linha de texto
    // --------------------------------------------------
    private static unsafe void RenderText(Shader shader, string text, float x, float y, float scale, Vector3 color)
    {
        // ativar o estado de renderização correspondente
        shader.Use();
        shader.SetVec3("textColor", color);

        _gl.ActiveTexture(TextureUnit.Texture0);

        _gl.BindVertexArray(VAO);

        // percorrer todos os caracteres
        foreach (char c in text)
        {
            Character ch = Characters[c];

            float xpos = x + ch.Bearing.X * scale;
            float ypos = y - (ch.Size.Y - ch.Bearing.Y) * scale;

            float w = ch.Size.X * scale;
            float h = ch.Size.Y * scale;

            // atualizar o VBO para cada caractere
            float[,] vertices = new float[6, 4]
            {
                { xpos,     ypos,     0.0f, 1.0f },
                { xpos + w, ypos,     1.0f, 1.0f },
                { xpos + w, ypos + h, 1.0f, 0.0f },
                { xpos,     ypos,     0.0f, 1.0f },
                { xpos + w, ypos + h, 1.0f, 0.0f },
                { xpos,     ypos + h, 0.0f, 0.0f }
            };

            // renderizar textura de glifo sobre quadrilátero
            _gl.BindTexture(TextureTarget.Texture2D, ch.TextureID);

            // atualizar o conteúdo da memória VBO
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, VBO);
            fixed (float* buf = vertices)
            {
                _gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (uint)(vertices.Length * sizeof(float)), buf); // certifique-se de usar glBufferSubData e não glBufferData
            }

            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

            // renderizar quadrilátero
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

            // agora avance os cursores para o próximo glifo (note que o avanço é em unidades de 1/64 de pixel)
            x += (ch.Advance >> 6) * scale; // deslocamento de bits de 6 posições para obter o valor em pixels (2^6 = 64 (divida a quantidade de 1/64 de pixel por 64 para obter a quantidade de pixels))
        }

        _gl.BindVertexArray(0);
        _gl.BindTexture(TextureTarget.Texture2D, 0);
    }
}
