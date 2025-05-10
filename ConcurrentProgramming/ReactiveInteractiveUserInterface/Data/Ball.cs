//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

namespace TP.ConcurrentProgramming.Data
{
  internal class Ball : IBall
  {
    #region ctor

    internal Ball(Vector initialPosition, Vector initialVelocity)
    {
      _Position = initialPosition;
      Velocity = initialVelocity;
      _mass = 1.0;
    }

    #endregion ctor

    #region IBall

    public event EventHandler<IVector>? NewPositionNotification;

    public IVector Velocity { get; set; }

    #endregion IBall

    #region private

    private double _mass;
    private Thread? _thread;
    private Vector _Position;

    private void RaiseNewPositionChangeNotification()
    {
      NewPositionNotification?.Invoke(this, _Position);
    }

    internal void Move(Vector delta)
    {
      _Position = new Vector(_Position.x + delta.x, _Position.y + delta.y);
      RaiseNewPositionChangeNotification();
    }

    #endregion private

    public double mass => _mass;
    internal void setPosition (Vector newPos)
    {
        _Position = newPos;
    }

    public void StartThread(Action<Ball> moveAction)
    {
      if (_thread != null)
      {
        throw new InvalidOperationException("Thread already started");
      }
      _thread = new Thread(ThreadFunction);
      _thread.Start(moveAction);
    }

    public void StopThread()
    {
    if (_thread == null)
    {
      throw new InvalidOperationException("Thread not started");
    }
    _thread.Interrupt();
      _thread.Join();
      _thread = null;
    }

    public void ThreadFunction(object? moveActionObject)
    {
      var moveAction = moveActionObject as Action<Ball>;
      if (moveAction == null)
      {
        throw new ArgumentNullException(nameof(moveAction));
      }
      try
      { 
        while(true)
        {
          Thread.Sleep(10);
          moveAction(this);
        }
      }
      catch (ThreadInterruptedException)
      {
        //konczymy watek
      }
    }
  }
}