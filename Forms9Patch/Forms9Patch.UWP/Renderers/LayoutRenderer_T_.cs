﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Media;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.UWP;

namespace Forms9Patch.UWP
{
    public class LayoutRenderer<TElement> : ViewRenderer<TElement, ImageView> where TElement : Layout //, IBackgroundImage
    {
        #region Fields
        bool _disposed;

        bool _debugMessages = false;

        static int _instances;
        int _instance;
        #endregion


        #region Constructor / Disposer
        public LayoutRenderer() => _instances = _instance++;

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                Control?.Dispose();
                SetNativeControl(null);
                _disposed = true;
            }

            base.Dispose(disposing);
        }
        #endregion


        #region Change management
        protected override async void OnElementChanged(ElementChangedEventArgs<TElement> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null)
            {
                SizeChanged -= OnSizeChanged;
                SetAutomationId(null);
            }

            if (e.NewElement != null)
            {
                if (Control == null)
                    SetNativeControl(new ImageView(_instance));
                SizeChanged += OnSizeChanged;

                if (((IBackgroundImage)Element)?.BackgroundImage != null)
                    await TryUpdateSource();
                UpdateAspect();
                UpdateCapInsets();

                UpdateClipToBounds();

                if (!string.IsNullOrEmpty(Element.AutomationId))
                {
                    SetAutomationId(Element.AutomationId);
                }
            }
        }

        protected override void UpdateBackgroundColor()
        {
            base.UpdateBackgroundColor();

            if (GetValue(BackgroundProperty) == null && Children.Count == 0)
            {
                // Forces the layout to take up actual space if it's otherwise empty
                Background = new SolidColorBrush(Colors.Transparent);
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            // Since layouts in Forms can be interacted with, we need to create automation peers
            // for them so we can interact with them in automated tests
            return new FrameworkElementAutomationPeer(this);
        }

        protected override async void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == Layout.IsClippedToBoundsProperty.PropertyName)
                UpdateClipToBounds();
            else if (e.PropertyName == Image.SourceProperty.PropertyName)
                await TryUpdateSource();
            else if (e.PropertyName == Image.FillProperty.PropertyName)
                UpdateAspect();
            else if (e.PropertyName == Image.CapInsetsProperty.PropertyName)
                UpdateCapInsets();


        }

        void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateClipToBounds();
        }

        void UpdateClipToBounds()
        {
            Clip = null;
            if (Element.IsClippedToBounds)
            {
                Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, ActualWidth, ActualHeight) };
            }
        }

        void RefreshImage()
        {
            if (_debugMessages) System.Diagnostics.Debug.WriteLine("LayoutRenderer<>[" + _instance + "].RefreshImage()");
            ((IVisualElementController)Element)?.InvalidateMeasure(InvalidationTrigger.RendererReady);
            if (_debugMessages) System.Diagnostics.Debug.WriteLine("LayoutRenderer<>[" + _instance + "].RefreshImage() RETURN ");
        }

        private void UpdateCapInsets()
        {
            if (Control != null && ((IBackgroundImage)Element)?.BackgroundImage != null)
                Control.CapInsets = ((IBackgroundImage)Element).BackgroundImage.CapInsets;
        }

        void UpdateAspect()
        {
            if (_debugMessages) System.Diagnostics.Debug.WriteLine("LayoutRenderer<>[" + _instance + "].UpdateAspect()");

            if (Control != null && ((IBackgroundImage)Element)?.BackgroundImage!=null)
                Control.Fill = ((IBackgroundImage)Element).BackgroundImage.Fill;

            if (_debugMessages) System.Diagnostics.Debug.WriteLine("LayoutRenderer<>[" + _instance + "].UpdateAspect() RETURN");
        }

        protected virtual async Task TryUpdateSource()
        {
            if (_debugMessages) System.Diagnostics.Debug.WriteLine("LayoutRenderer<>[" + _instance + "].TryUpdateSource()");
            // By default we'll just catch and log any exceptions thrown by UpdateSource so we don't bring down
            // the application; a custom renderer can override this method and handle exceptions from
            // UpdateSource differently if it wants to

            try
            {
                await UpdateSource().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(nameof(ImageRenderer), "LayoutRenderer<>[" + _instance + "].TryUpdateSource() Error loading image: {0}", ex);
            }
            finally
            {
                //((IImageController)Element)?.SetIsLoading(false);
                ((IImageController)((IBackgroundImage)Element)?.BackgroundImage)?.SetIsLoading(false);
            }
            if (_debugMessages) System.Diagnostics.Debug.WriteLine("LayoutRenderer<>[" + _instance + "].TryUpdateSource() RETURN");
        }

        protected async Task UpdateSource()
        {
            if (_debugMessages) System.Diagnostics.Debug.WriteLine("LayoutRenderer<>[" + _instance + "].UpdateSource()");
            if (_disposed || Element == null || Control == null)
            {
                System.Diagnostics.Debug.WriteLine("LayoutRenderer<>[" + _instance + "].UpdateSource() RETURN");
                return;
            }


            //Element.SetIsLoading(true);
            //((IElementController)Element).SetValueFromRenderer(Xamarin.Forms.Image.IsLoadingProperty, true);
                ((IImageController)((IBackgroundImage)Element).BackgroundImage).SetIsLoading(true);

            Xamarin.Forms.ImageSource source = ((IBackgroundImage)Element)?.BackgroundImage?.Source;

            await Control.SetSourceAsync(source);

            RefreshImage();



            //Element.SetIsLoading(false);
            //((IElementController)Element).SetValueFromRenderer(Xamarin.Forms.Image.IsLoadingProperty, false);
            ((IImageController)((IBackgroundImage)Element).BackgroundImage).SetIsLoading(false);


        }

        Windows.Foundation.Size _lastFinalSize = Windows.Foundation.Size.Empty;
        protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
        {
            
            if (_debugMessages) System.Diagnostics.Debug.WriteLine("LayoutRenderer<>[" + _instance + "].ArrangeOverride(" + finalSize + ") ENTER/RETURN");

            if (((IBackgroundImage)Element)?.BackgroundImage!=null &&  _lastFinalSize != finalSize && ((IBackgroundImage)Element).BackgroundImage.Fill == Fill.Tile && finalSize.Width > 0 && finalSize.Height > 0 && !Double.IsInfinity(finalSize.Width) && !Double.IsInfinity(finalSize.Height) && !Double.IsNaN(finalSize.Width) && !Double.IsNaN(finalSize.Height))
                Control.GenerateLayout(finalSize);
            _lastFinalSize = finalSize;
            return base.ArrangeOverride(finalSize);
        }

        #endregion
    }
}