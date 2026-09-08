/*******************************************************************
** This code is part of Breakout.
**
** Breakout is free software: you can redistribute it and/or modify
** it under the terms of the CC BY 4.0 license as published by
** Creative Commons, either version 4 of the License, or (at your
** option) any later version.
******************************************************************/

using System.Numerics;

namespace Breakout.src;

// PowerUp herda seu estado e suas funções de renderização de
// GameObject, mas também armazena informações adicionais sobre
// sua duração de atividade e se está ativado ou não.
// O tipo de PowerUp é armazenado como uma string.
public class PowerUp : GameObject
{
    // O tamanho de um bloco PowerUp
    private static Vector2 POWERUP_SIZE = new Vector2(60.0f, 20.0f);

    // Velocidade que um bloco PowerUp possui ao ser gerado
    private static Vector2 VELOCITY = new Vector2(0.0f, 150.0f);

    // estado do power-up
    public string Type;
    public float Duration;
    public bool Activated;

    // construtor
    public PowerUp(string type, Vector3 color, float duration, Vector2 position, Texture2D texture) : base(position, POWERUP_SIZE, texture, color, VELOCITY)
    {
        Type = type;
        Duration = duration;
    }
}
