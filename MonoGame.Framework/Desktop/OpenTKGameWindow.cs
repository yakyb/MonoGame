﻿#region License
/*
Microsoft Public License (Ms-PL)
XnaTouch - Copyright © 2009 The XnaTouch Team

All rights reserved.

This license governs use of the accompanying software. If you use the software, you accept this license. If you do not
accept the license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under 
U.S. copyright law.

A "contribution" is the original software, or any additions or changes to the software.
A "contributor" is any person that distributes its contribution under this license.
"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

3. Conditions and Limitations
(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
your patent license from such contributor to the software ends automatically.
(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution 
notices that are present in the software.
(D) If you distribute any portion of the software in source code form, you may do so only under this license by including 
a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object 
code form, you may only do so under a license that complies with this license.
(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees
or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent
permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular
purpose and non-infringement.
*/
#endregion License

#region Using Statements
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenTK;
using OpenTK.Graphics;


#endregion Using Statements

namespace Microsoft.Xna.Framework
{
    public class OpenTKGameWindow : GameWindow
    {
        private GameTime _updateGameTime;
        private GameTime _drawGameTime;
        private DateTime _lastUpdate;
        private DateTime _now;
        private bool _allowUserResizing;
        private DisplayOrientation _currentOrientation;
        private OpenTK.GameWindow window;
        protected Game game;
        private List<Microsoft.Xna.Framework.Input.Keys> keys;

        // we need this variables to make changes beetween threads
        private WindowState windowState;
        private Rectangle clientBounds;
        private bool updateClientBounds;

        #region Internal Properties

        internal Game Game { get; set; }

        internal OpenTK.GameWindow Window { get { return window; } }

        #endregion

        #region Public Properties

        public override IntPtr Handle { get { return IntPtr.Zero; } }

        public override string ScreenDeviceName { get { return window.Title; } }

        public override Rectangle ClientBounds { get { return clientBounds; } }

        public override string Title
        {
            get { return window.Title; }
            set { SetTitle(value); }
        }

        // TODO: this is buggy on linux - report to opentk team
        public override bool AllowUserResizing
        {
            get { return _allowUserResizing; }
            set
            {
                _allowUserResizing = value;
                if (_allowUserResizing)
                    window.WindowBorder = WindowBorder.Resizable;
                else
                    window.WindowBorder = WindowBorder.Fixed; // OTK's buggy here, let's wait for 1.1
            }
        }

        public override DisplayOrientation CurrentOrientation
        {
            get
            {
                return _currentOrientation;
            }
            internal set
            {
                if (value != _currentOrientation)
                {
                    _currentOrientation = value;
                    OnOrientationChanged();
                }
            }
        }

        #endregion

        public OpenTKGameWindow()
        {
            Initialize();
        }

        #region Restricted Methods

        #region OpenTK GameWindow Methods

        #region Delegates

        private void OpenTkGameWindow_Closing(object sender, CancelEventArgs e)
        {
            Game.Exit();
        }

        private void Keyboard_KeyUp(object sender, OpenTK.Input.KeyboardKeyEventArgs e)
        {
            Keys xnaKey = KeyboardUtil.ToXna(e.Key);
            if (keys.Contains(xnaKey)) keys.Remove(xnaKey);
        }

        private void Keyboard_KeyDown(object sender, OpenTK.Input.KeyboardKeyEventArgs e)
        {
            Keys xnaKey = KeyboardUtil.ToXna(e.Key);
            if (!keys.Contains(xnaKey)) keys.Add(xnaKey);
        }

        protected void OnActivated() { }

        protected void OnClientSizeChanged()
        {
            if (ClientSizeChanged != null)
            {
                ClientSizeChanged(this, EventArgs.Empty);
            }
        }

        protected void OnDeactivated() { }

        protected void OnOrientationChanged()
        {
            if (OrientationChanged != null)
            {
                OrientationChanged(this, EventArgs.Empty);
            }
        }

        protected void OnPaint() { }

        protected void OnScreenDeviceNameChanged()
        {
            if (ScreenDeviceNameChanged != null)
            {
                ScreenDeviceNameChanged(this, EventArgs.Empty);
            }
        }

        #endregion

        private void OnResize(object sender, EventArgs e)
        {
            //Game.GraphicsDevice.SizeChanged(window.ClientRectangle.Width, window.ClientRectangle.Height);
            Game.GraphicsDevice.Viewport = new Viewport(0, 0, window.ClientRectangle.Width, window.ClientRectangle.Height);
            OnClientSizeChanged();
        }

        private void OnRenderFrame(object sender, FrameEventArgs e)
        {
            if (GraphicsContext.CurrentContext == null || GraphicsContext.CurrentContext.IsDisposed)
                return;

            //Should not happen at all..
            if (!GraphicsContext.CurrentContext.IsCurrent)
                window.MakeCurrent();

            // we should wait until window's not fullscreen to resize
            if (updateClientBounds && window.WindowState == WindowState.Normal)
            {
                window.ClientRectangle = new System.Drawing.Rectangle(clientBounds.X,
                                     clientBounds.Y, clientBounds.Width, clientBounds.Height);

                updateClientBounds = false;
            }

            if (window.WindowState != windowState)
                window.WindowState = windowState;
        }

        private void OnUpdateFrame(object sender, FrameEventArgs e)
        {
            if (Game != null)
            {
                HandleInput();
                Game.Tick();
            }
        }

        private void HandleInput()
        {
            // mouse doesn't need to be treated here, Mouse class does it alone

            // keyboard
            Keyboard.State = new KeyboardState(keys.ToArray());
        }

        #endregion

        private void Initialize()
        {
            window = new OpenTK.GameWindow();
            window.RenderFrame += OnRenderFrame;
            window.UpdateFrame += OnUpdateFrame;
            window.Closing += new EventHandler<CancelEventArgs>(OpenTkGameWindow_Closing);
            window.Resize += OnResize;
            window.Keyboard.KeyDown += new EventHandler<OpenTK.Input.KeyboardKeyEventArgs>(Keyboard_KeyDown);
            window.Keyboard.KeyUp += new EventHandler<OpenTK.Input.KeyboardKeyEventArgs>(Keyboard_KeyUp);

            // Set the window icon.
            window.Icon = Icon.ExtractAssociatedIcon(Assembly.GetEntryAssembly().Location);

            updateClientBounds = false;
            clientBounds = new Rectangle(window.ClientRectangle.X, window.ClientRectangle.Y,
                                         window.ClientRectangle.Width, window.ClientRectangle.Height);
            windowState = window.WindowState;

            keys = new List<Keys>();

            // mouse
            // TODO review this when opentk 1.1 is released
#if !WINDOWS
            Mouse.UpdateMouseInfo(window.Mouse);
#else
            Mouse.setWindows(window);
#endif

            // Initialize GameTime
            _updateGameTime = new GameTime();
            _drawGameTime = new GameTime();

            // Initialize _lastUpdate
            _lastUpdate = DateTime.Now;

            //Default no resizing
            AllowUserResizing = false;            
        }

        protected override void SetTitle(string title)
        {
            window.Title = title;
        }

        internal void Run(double updateRate)
        {
            window.Run(updateRate);
        }

        internal void ToggleFullScreen()
        {
            if (windowState == WindowState.Fullscreen)
                windowState = WindowState.Normal;
            else
                windowState = WindowState.Fullscreen;
        }

        internal void ChangeClientBounds(Rectangle clientBounds)
        {
            updateClientBounds = true;
            this.clientBounds = clientBounds;
        }

        #endregion

        #region Public Methods

        public void Dispose()
        {
            window.Dispose();
        }

        public override void BeginScreenDeviceChange(bool willBeFullScreen)
        {
        }

        public override void EndScreenDeviceChange(string screenDeviceName, int clientWidth, int clientHeight)
        {

        }

        public override void EndScreenDeviceChange(string screenDeviceName) { }

        #endregion

        #region Events

        public event EventHandler<EventArgs> ClientSizeChanged;
        public event EventHandler<EventArgs> OrientationChanged;
        public event EventHandler<EventArgs> ScreenDeviceNameChanged;

        #endregion
    }
}
