﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino;
using Rhino.Geometry;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace examples_cs
{
  [System.Runtime.InteropServices.Guid("E4A93905-6E61-43BB-9FF0-4D5F6AF76704")]
  public class ex_dotneteventwatcher : Rhino.Commands.Command
  {
    public override string EnglishName { get { return "csEventWatcher"; } }
    private RhinoDoc _doc;
    private Label _label;
    private Window _window;

    protected override Rhino.Commands.Result RunCommand(RhinoDoc doc, Rhino.Commands.RunMode mode)
    {
      _doc = doc;

      _window = new Window();
      _window.Title = "Object ID and Thread ID";
      _window.Width = 500;
      _window.Height = 75;
      _label = new Label();
      _window.Content = _label;
      new System.Windows.Interop.WindowInteropHelper(_window).Owner = Rhino.RhinoApp.MainWindowHandle();
      _window.Show();


      // register the rhinoObjectAdded method with the AddRhinoObject event
      RhinoDoc.AddRhinoObject += rhinoObjectAdded;

      // add a sphere from the main UI thread.  All is good
      addSphere(new Point3d(0,0,0));

      // add a sphere from a secondary thread. Not good: the rhinoObjectAdded method
      // doesn't work well when called from another thread
      var addSphereDelegate = new Action<Point3d>(addSphere);
      addSphereDelegate.BeginInvoke(new Point3d(0, 10, 0), null, null);

      // handle the AddRhinoObject event with rhinoObjectAddedSafe which is
      // desgined to work no matter which thread the call is comming from.
      RhinoDoc.AddRhinoObject -= rhinoObjectAdded;
      RhinoDoc.AddRhinoObject += rhinoObjectAddedSafe;

      // try again adding a sphere from a secondary thread.  All is good!
      addSphereDelegate.BeginInvoke(new Point3d(0, 20, 0), null, null);

      doc.Views.Redraw();

      return Rhino.Commands.Result.Success;
    }

    private void addSphere(Point3d center) {
      _doc.Objects.AddSphere(new Sphere(center, 3));
    }

    private void rhinoObjectAdded(Object sender, Rhino.DocObjects.RhinoObjectEventArgs e)
    {
      var msg = String.Format("thread id = {0}, obj id = {1}",
            Thread.CurrentThread.ManagedThreadId,
            e.ObjectId.ToString());

      RhinoApp.WriteLine(msg);

      try {
        // when a sphere is added from a secondary thread this line will
        // throw an exception because UI controls can only be accessed from
        // the main UI thread
        _label.Content = msg;
      } catch (InvalidOperationException ioe) {RhinoApp.WriteLine(ioe.Message);}
    }

    private void rhinoObjectAddedSafe(Object sender, Rhino.DocObjects.RhinoObjectEventArgs e)
    {
      var msg = String.Format("thread id = {0}, obj id = {1}",
            Thread.CurrentThread.ManagedThreadId,
            e.ObjectId.ToString());

      RhinoApp.WriteLine(msg);

      // checks if the calling thread is the thread the dispatcher is associated with.
      // In other words, checks if the calling thread is the UI thread
      if (_label.Dispatcher.CheckAccess())
        // if we're on the UI thread then just update the component
        _label.Content = msg;
      else
      {
        // invoke the setLabelTextDelegate on the thread the dispatcher is associated with, i.e., the UI thread
        var setLabelTextDelegate = new Action<string>(txt => _label.Content = txt);
        _label.Dispatcher.BeginInvoke(setLabelTextDelegate, new String[] { msg });
      }
    }
  }
}
