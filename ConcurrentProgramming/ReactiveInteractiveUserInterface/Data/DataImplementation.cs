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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Timers;
using System.Xml.Serialization;

namespace TP.ConcurrentProgramming.Data
{
  [Serializable]
  public class BallState
  {
    public double X { get; set; }
    public double Y { get; set; }
    public double Vx { get; set; }
    public double Vy { get; set; }
  }
  internal class DataImplementation : DataAbstractAPI
  {
    internal DataImplementation()
    {
        double pad = (tableWidth - gridSize * 2 * ballRadius) / (gridSize + 1);
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
               double x = ballRadius + pad + i * (2*ballRadius + pad);
               double y = ballRadius + pad + j * (2*ballRadius + pad);
               StartingPositions[i * gridSize + j] = new Vector(x, y);
            }
        }
        StartingPositions = StartingPositions
                    .OrderBy(_ => random.Next())
                    .ToArray();
        _saveTimer = new System.Timers.Timer(interval: 60000); // minuta
        _saveTimer.AutoReset = true;
        _saveTimer.Elapsed += OnSaveTimerElapsed;
        _saveTimer.Start();
    }

    #region DataAbstractAPI
    public override IVector CreateVector(double x, double y)
    {
        return new Vector(x, y);
    }
    public override void ChangePos(IBall ib, IVector v)
    {
        Ball b = (Ball)ib;
        b.setPosition((Vector)v);
    }
    public override void Start(int numberOfBalls, Action<IVector, IBall> upperLayerHandler)
    {
      if (Disposed)
        throw new ObjectDisposedException(nameof(DataImplementation));
      if (upperLayerHandler == null)
        throw new ArgumentNullException(nameof(upperLayerHandler));
      lock (BallsList)
      {
		foreach (Ball ball in BallsList)
		{
          ball.StopThread();
		}
		BallsList.Clear();
        for (int i = 0; i < numberOfBalls; i++)
        {
          Vector startingPosition = StartingPositions[i];
          double angle = 2 * Math.PI * random.NextDouble();
          double speed = 1.0;
          double vx = speed * Math.Cos(angle);
          double vy = speed * Math.Sin(angle);
          Vector initialVelocity = new Vector(vx, vy);

          Ball newBall = new(startingPosition, initialVelocity);
          upperLayerHandler(startingPosition, newBall);
          BallsList.Add(newBall);
        }
		foreach (Ball ball in BallsList)
		{
		  ball.StartThread(this.Move);
		}
      }
      OnSaveTimerElapsed(null, null); // zapis na starcie
    }

    #endregion DataAbstractAPI

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            if (disposing)
            {
                _saveTimer.Stop();
                _saveTimer.Dispose();
                lock (BallsList)
                {
                    foreach (Ball ball in BallsList)
                    {
                        ball.StopThread();
                    }
                    BallsList.Clear();
                }
            }
            Disposed = true;
        }
        else
        { throw new ObjectDisposedException(nameof(DataImplementation)); }
    }

    public override void Dispose()
    {
      // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }

    #endregion IDisposable

    #region private

    private readonly System.Timers.Timer _saveTimer;
    private string _outputFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "balls_state.xml");
    //private bool disposedValue;
    private bool Disposed = false;
    private List<Ball> BallsList = [];
    private const int gridSize = 18;
    private Vector[] StartingPositions = new Vector[gridSize*gridSize];
    private const double tableWidth = 400.0;
    private const double tableHeight = 400.0;
    private const double ballRadius = 10.0;
    private Random random = new Random();
    private void Move(Ball ball)
    {
      lock (BallsList)
      {
        ball.Move(ball.Velocity);  
      }
    }
    private void OnSaveTimerElapsed(object? sender, ElapsedEventArgs? e)
    {
        List<BallState> snapshot = new List<BallState>();
        lock (BallsList)
        {
            foreach (var ball in BallsList)
            {
                Vector pos = ball.Position;
                Vector vel = (Vector)ball.Velocity;
                snapshot.Add(new BallState
                {
                    X = pos.x,
                    Y = pos.y,
                    Vx = vel.x,
                    Vy = vel.y
                });
            }
        }
        try
        {
            using (FileStream fs = new(_outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                XmlSerializer serializer = new(typeof(List<BallState>));
                serializer.Serialize(fs, snapshot);
            }
        }
        catch (IOException ioEx)
        {
            Debug.WriteLine($"Failed to serialize balls: {ioEx.Message}");
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