using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Application = System.Windows.Application;

namespace VoiceMeeterVolumeConfiguration.Wrappers;

public class NotifyIconWrapper : FrameworkElement, IDisposable
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register("Text", typeof(string), typeof(NotifyIconWrapper), new PropertyMetadata(
            (d, e) =>
            {
                var notifyIcon = ((NotifyIconWrapper)d)._notifyIcon;
                if (notifyIcon == null)
                    return;
                notifyIcon.Text = (string)e.NewValue;
            }));

    private static readonly DependencyProperty NotifyRequestProperty =
        DependencyProperty.Register("NotifyRequest", typeof(NotifyRequestRecord), typeof(NotifyIconWrapper),
            new PropertyMetadata(
                (d, e) =>
                {
                    var r = (NotifyRequestRecord)e.NewValue;
                    ((NotifyIconWrapper)d)._notifyIcon?.ShowBalloonTip(r.Duration, r.Title, r.Text, r.Icon);
                }));

    private readonly NotifyIcon? _notifyIcon;

    public NotifyIconWrapper()
    {
        if (DesignerProperties.GetIsInDesignMode(this))
            return;
        this._notifyIcon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
            Visible = true,
            ContextMenuStrip = this.CreateContextMenu()
        };
        this._notifyIcon.Click += this.OnClick;
        this._notifyIcon.DoubleClick += this.OnDoubleClick;
        Application.Current.Exit += (obj, args) => { this._notifyIcon.Dispose(); };
    }

    public string Text
    {
        get => (string)this.GetValue(TextProperty);
        set => this.SetValue(TextProperty, value);
    }

    public NotifyRequestRecord NotifyRequest
    {
        get => (NotifyRequestRecord)this.GetValue(NotifyRequestProperty);
        set => this.SetValue(NotifyRequestProperty, value);
    }

    public void Dispose()
    {
        this._notifyIcon?.Dispose();
    }

    public static readonly DependencyProperty ClickCommandProperty = DependencyProperty.Register(
        "ClickCommand", typeof(ICommand), typeof(NotifyIconWrapper), new PropertyMetadata(default(ICommand)));

    public static readonly DependencyProperty ExitCommandProperty = DependencyProperty.Register(
        "ExitCommand", typeof(ICommand), typeof(NotifyIconWrapper), new PropertyMetadata(default(ICommand)));

    public static readonly DependencyProperty DoubleClickCommandProperty = DependencyProperty.Register(
        "DoubleClickCommand", typeof(ICommand), typeof(NotifyIconWrapper), new PropertyMetadata(default(ICommand)));

    public static readonly DependencyProperty OpenCommandProperty = DependencyProperty.Register(
        "OpenCommand", typeof(ICommand), typeof(NotifyIconWrapper), new PropertyMetadata(default(ICommand)));

    public ICommand? OpenCommand
    {
        get => (ICommand)this.GetValue(OpenCommandProperty);
        set => this.SetValue(OpenCommandProperty, value);
    }
    
    public ICommand? DoubleClickCommand
    {
        get => (ICommand)this.GetValue(DoubleClickCommandProperty);
        set => this.SetValue(DoubleClickCommandProperty, value);
    }
    
    public ICommand? ExitCommand
    {
        get => (ICommand)this.GetValue(ExitCommandProperty);
        set => this.SetValue(ExitCommandProperty, value);
    }

    public ICommand? ClickCommand
    {
        get => (ICommand)this.GetValue(ClickCommandProperty);
        set => this.SetValue(ClickCommandProperty, value);
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var openItem = new ToolStripMenuItem("Open");
        openItem.Click += this.OpenItemOnClick;
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += this.ExitItemOnClick;
        var contextMenu = new ContextMenuStrip { Items = { openItem, exitItem } };
        return contextMenu;
    }

    private void OpenItemOnClick(object? sender, EventArgs eventArgs)
    {
        this.OpenCommand?.Execute(eventArgs);
    }
    
    private void OnClick(object? sender, EventArgs eventArgs)
    {
        this.ClickCommand?.Execute(eventArgs);
    }
    
    private void OnDoubleClick(object? sender, EventArgs eventArgs)
    {
        this.DoubleClickCommand?.Execute(eventArgs);
    }

    private void ExitItemOnClick(object? sender, EventArgs eventArgs)
    {
        this.ExitCommand?.Execute(eventArgs);
    }

    public class NotifyRequestRecord
    {
        public string Title { get; set; } = "";
        public string Text { get; set; } = "";
        public int Duration { get; set; } = 1000;
        public ToolTipIcon Icon { get; set; } = ToolTipIcon.Info;
    }
}