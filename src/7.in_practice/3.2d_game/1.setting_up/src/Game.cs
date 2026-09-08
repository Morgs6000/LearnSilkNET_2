/*******************************************************************
** This code is part of Breakout.
**
** Breakout is free software: you can redistribute it and/or modify
** it under the terms of the CC BY 4.0 license as published by
** Creative Commons, either version 4 of the License, or (at your
** option) any later version.
******************************************************************/

using System.Text.Json;

namespace Breakout.src;

// Representa o estado atual do jogo
public enum GameState
{
    GAME_ACTIVE,
    GAME_MENU,
    GAME_WIN
}

// A classe Game encapsula todo o estado e a funcionalidade relacionados ao jogo.
// Ela reúne todos os dados do jogo em uma única classe para
// facilitar o acesso aos componentes e o gerenciamento.
public class Game 
{
    // estado do jogo
    public GameState State;
    public bool[] Keys = new bool[1024];
    public uint Widht, Height;

    // construtor
    public Game(uint width, uint height)
    {
        State = GameState.GAME_ACTIVE;
        
        Widht = width;
        Height = height;
    }

    // desconstrutor
    ~Game()
    {
        
    }

    // inicializar o estado do jogo (carregar todos os shaders/texturas/níveis)
    public void Init()
    {
        
    }

    // loop do jogo
    public void ProcessInput(float dt)
    {
        
    }

    public void Update(float dt)
    {
        
    }

    public void Render()
    {
        
    }
}
