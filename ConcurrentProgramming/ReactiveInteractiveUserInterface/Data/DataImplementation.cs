//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

using System;
using System.Diagnostics;

namespace TP.ConcurrentProgramming.Data
{
  internal class DataImplementation : DataAbstractAPI
  {
    #region ctor

    public DataImplementation()
    {
      MoveTimer = new Timer(Move, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
    }

    #endregion ctor

    private const double tableWidth = 400.0;
    private const double tableHeight = 400.0;
    private const double ballRadius = 10.0; // smelly, definicja średnicy jest w warstwach wyżej
    private const double frameTime = 0.1;

    #region DataAbstractAPI

    public override void Start(int numberOfBalls, Action<IVector, IBall> upperLayerHandler)
    {
      if (Disposed)
        throw new ObjectDisposedException(nameof(DataImplementation));
      if (upperLayerHandler == null)
        throw new ArgumentNullException(nameof(upperLayerHandler));
      Random random = new Random();
      for (int i = 0; i < numberOfBalls; i++)
      {
        Vector startingPosition = new(random.Next(100, tableWidth - 100), random.Next(100, tableHeight - 100));
        double angle = 2 * Math.PI * random.NextDouble();
        double speed = 5.0;
        double vx = speed * Math.Cos(angle);
        double vy = speed * Math.Sin(angle);
        Vector initialVelocity = new Vector(vx, vy);

        Ball newBall = new(startingPosition, initialVelocity);
        upperLayerHandler(startingPosition, newBall);
        BallsList.Add(newBall);
      }
    }

    #endregion DataAbstractAPI

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
      if (!Disposed)
      {
        if (disposing)
        {
          MoveTimer.Dispose();
          BallsList.Clear();
        }
        Disposed = true;
      }
      else
        throw new ObjectDisposedException(nameof(DataImplementation));
    }

    public override void Dispose()
    {
      // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }

    #endregion IDisposable

    #region private

    //private bool disposedValue;
    private bool Disposed = false;

    private readonly Timer MoveTimer;
    private Random RandomGenerator = new();
    private List<Ball> BallsList = [];

    private void Move(object? x)
    {
        foreach (Ball ball in BallsList)
        {
            ball.Move(new Vector(ball.Velocity.x * frameTime, ball.Velocity.y * frameTime));
            Vector position = ball.Position;
            if ((position.x - ballRadius <= 0 && ball.Velocity.x < 0) || (position.x + ballRadius >= tableWidth && ball.Velocity.x > 0))
            {
                ball.Velocity = new Vector(-ball.Velocity.x, ball.Velocity.y);
            }
            if ((position.y - ballRadius <= 0 && ball.Velocity.y < 0) || (position.y + ballRadius >= tableHeight && ball.Velocity.y > 0))
            {
                ball.Velocity = new Vector(ball.Velocity.x, -ball.Velocity.y);
            }
        }
        for (int i = 0; i < BallsList.Count; i++)
        {
            for (int j = i + 1; j < BallsList.Count; j++)
            {
                Ball ball1 = BallsList[i];
                Ball ball2 = BallsList[j];

                Vector delta = new Vector(
                    ball2.Position.x - ball1.Position.x,
                    ball2.Position.y - ball1.Position.y);

                double distance = Math.Sqrt(delta.x * delta.x + delta.y * delta.y);

                if (distance < 2 * ballRadius) // kolizja
                {
                    // wyznaczenie wektora normalnego (kierunek kolizji)
                    Vector normal = new Vector(delta.x / distance, delta.y / distance);

                    // obliczenie względnej prędkości
                    Vector relativeVelocity = new Vector(
                        ball1.Velocity.x - ball2.Velocity.x,
                        ball1.Velocity.y - ball2.Velocity.y);

                    // rzut względnej prędkości na kierunek normalny
                    double dot = relativeVelocity.x * normal.x + relativeVelocity.y * normal.y;

                    // odbicie tylko gdy kulki poruszają się w kierunku siebie
                    if (dot < 0)
                    {
                        ball1.Velocity = new Vector(
                                ball1.Velocity.x - dot * normal.x,
                                ball1.Velocity.y - dot * normal.y);

                        ball2.Velocity = new Vector(
                                ball2.Velocity.x + dot * normal.x,
                                ball2.Velocity.y + dot * normal.y);
                    }
                }
            }
        }
    }
    #endregion private

    #region TestingInfrastructure

    [Conditional("DEBUG")]
    internal void CheckBallsList(Action<IEnumerable<IBall>> returnBallsList)
    {
      returnBallsList(BallsList);
    }

    [Conditional("DEBUG")]
    internal void CheckNumberOfBalls(Action<int> returnNumberOfBalls)
    {
      returnNumberOfBalls(BallsList.Count);
    }

    [Conditional("DEBUG")]
    internal void CheckObjectDisposed(Action<bool> returnInstanceDisposed)
    {
      returnInstanceDisposed(Disposed);
    }

    #endregion TestingInfrastructure
  }
}