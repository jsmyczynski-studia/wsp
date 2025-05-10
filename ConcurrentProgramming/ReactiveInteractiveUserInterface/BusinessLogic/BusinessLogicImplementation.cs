//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

using System.Diagnostics;
using System.Numerics;
using TP.ConcurrentProgramming.Data;
using UnderneathLayerAPI = TP.ConcurrentProgramming.Data.DataAbstractAPI;

namespace TP.ConcurrentProgramming.BusinessLogic
{
  internal class BusinessLogicImplementation : BusinessLogicAbstractAPI
  {
    #region ctor

    public BusinessLogicImplementation() : this(null)
    { }

    internal BusinessLogicImplementation(UnderneathLayerAPI? underneathLayer)
    {
      layerBellow = underneathLayer == null ? UnderneathLayerAPI.GetDataLayer() : underneathLayer;
    }

    #endregion ctor

    #region BusinessLogicAbstractAPI

    public override void Dispose()
    {
      if (Disposed)
        throw new ObjectDisposedException(nameof(BusinessLogicImplementation));
      layerBellow.Dispose();
      Disposed = true;
    }

    public override void Start(int numberOfBalls, Action<IPosition, IBall> upperLayerHandler)
    {
      if (Disposed)
        throw new ObjectDisposedException(nameof(BusinessLogicImplementation));
      if (upperLayerHandler == null)
        throw new ArgumentNullException(nameof(upperLayerHandler));
      lock (dataBalls)
      {
          dataBalls.Clear();
          lastPositions.Clear();
      }
      layerBellow.Start(numberOfBalls, (startingPosition, dataBall) =>
      {
          lock (dataBalls)
          {
              dataBalls.Add(dataBall);
              lastPositions[dataBall] = startingPosition;
              dataBall.NewPositionNotification += OnDataBallMoved;
          }
          var businessBall = new Ball(dataBall);
          upperLayerHandler(
              new Position(startingPosition.x, startingPosition.y),
              businessBall);
      });
    }
    private void OnDataBallMoved(object? sender, Data.IVector pos)
    {
        var ball = (Data.IBall)sender!;
        var lastPos = lastPositions[ball];
        lock (dataBalls)
        {
            lastPositions[ball] = pos;
            if ((pos.x - ballRadius <= 0 && ball.Velocity.x < 0) || (pos.x + ballRadius >= tableWidth && ball.Velocity.x > 0))
            {
                ball.Velocity = layerBellow.CreateVector(-ball.Velocity.x, ball.Velocity.y);
                layerBellow.ChangePos(ball, lastPos);
                lastPositions[ball] = lastPos;
                return;
            }
            if ((pos.y - ballRadius <= 0 && ball.Velocity.y < 0) || (pos.y + ballRadius >= tableHeight && ball.Velocity.y > 0))
            {
                ball.Velocity = layerBellow.CreateVector(ball.Velocity.x, -ball.Velocity.y);
                layerBellow.ChangePos(ball, lastPos);
                lastPositions[ball] = lastPos;
                return;
            }
            foreach (var otherBall in dataBalls)
            {
                if (ReferenceEquals(otherBall, ball))
                {
                    continue;
                }
                var deltaDist = layerBellow.CreateVector(
                    lastPositions[otherBall].x - pos.x,
                    lastPositions[otherBall].y - pos.y);
                double distance1 = Math.Sqrt(deltaDist.x * deltaDist.x + deltaDist.y * deltaDist.y);
                if (distance1 < 2 * ballRadius) // kolizja
                {
                    var deltaNorm = layerBellow.CreateVector(
                        lastPositions[otherBall].x - lastPos.x,
                        lastPositions[otherBall].y - lastPos.y);
                    double distance = Math.Sqrt(deltaNorm.x * deltaNorm.x + deltaNorm.y * deltaNorm.y);
                    // wyznaczenie wektora normalnego (kierunek kolizji)
                    IVector normal = layerBellow.CreateVector(deltaNorm.x / distance, deltaNorm.y / distance);
                    IVector tangent = layerBellow.CreateVector(-normal.y, normal.x); // wektor styczny
                    double v1n = ball.Velocity.x * normal.x + ball.Velocity.y * normal.y; // prędkość w kierunku normalnym
                    double v2n = otherBall.Velocity.x * normal.x + otherBall.Velocity.y * normal.y;
                    double v1t = ball.Velocity.x * tangent.x + ball.Velocity.y * tangent.y; // prędkość w kierunku stycznym
                    double v2t = otherBall.Velocity.x * tangent.x + otherBall.Velocity.y * tangent.y;
                    // uwzglednianie mas
                    double m1 = ball.mass;
                    double m2 = otherBall.mass;
                    double newV1n = (v1n * (m1 - m2) + 2 * m2 * v2n) / (m1 + m2);
                    double newV2n = (v2n * (m2 - m1) + 2 * m1 * v1n) / (m1 + m2);
                    // nowa prędkość w kierunku normalnym
                    double newV1x = (newV1n * normal.x) + (v1t * tangent.x);
                    double newV1y = (newV1n * normal.y) + (v1t * tangent.y);
                    double newV2x = (newV2n * normal.x) + (v2t * tangent.x);
                    double newV2y = (newV2n * normal.y) + (v2t * tangent.y);
                    ball.Velocity = layerBellow.CreateVector(newV1x, newV1y);
                    otherBall.Velocity = layerBellow.CreateVector(newV2x, newV2y);
                    layerBellow.ChangePos(ball, lastPos);
                    lastPositions[ball] = lastPos;
                    return;
                }
            }
        }
    }

    #endregion BusinessLogicAbstractAPI

    #region private

    private bool Disposed = false;
    private readonly UnderneathLayerAPI layerBellow;
    private readonly object businessLogicLock = new object();
    private readonly List<Data.IBall> dataBalls = [];
    private readonly Dictionary<Data.IBall, Data.IVector> lastPositions = new();
    private const double ballRadius = 10.0;
    private const double tableWidth = 400.0;
    private const double tableHeight = 400.0;

    #endregion private

    #region TestingInfrastructure

    [Conditional("DEBUG")]
    internal void CheckObjectDisposed(Action<bool> returnInstanceDisposed)
    {
      returnInstanceDisposed(Disposed);
    }

    #endregion TestingInfrastructure
  }
}