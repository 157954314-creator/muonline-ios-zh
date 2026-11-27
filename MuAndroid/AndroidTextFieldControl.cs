#nullable enable
using Android.App;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Client.Main;
using Client.Main.Controls.UI;
using System;
using System.Threading;

namespace MuAndroid
{
    public class AndroidTextFieldControl : TextFieldControl
    {
        private int _dialogOpen;

        public override void OnFocus()
        {
            base.OnFocus();

            var activity = MainActivity.Instance;
            if (activity == null || activity.IsFinishing || activity.IsDestroyed)
                return;

            if (Interlocked.Exchange(ref _dialogOpen, 1) != 0)
                return;

            string title = !string.IsNullOrWhiteSpace(Label)
                ? Label
                : (MaskValue ? "请输入密码" : "请输入内容");
            string hint = !string.IsNullOrWhiteSpace(Placeholder)
                ? Placeholder
                : (MaskValue ? "输入密码" : "点击输入");
            string currentValue = Value ?? string.Empty;
            bool isPassword = MaskValue;

            activity.RunOnUiThread(() =>
            {
                try
                {
                    if (activity.IsFinishing || activity.IsDestroyed)
                    {
                        Interlocked.Exchange(ref _dialogOpen, 0);
                        return;
                    }

                    var container = new FrameLayout(activity);
                    float density = activity.Resources!.DisplayMetrics!.Density;
                    int padX = (int)(20 * density);
                    int padY = (int)(12 * density);
                    int innerPad = (int)(10 * density);
                    container.SetPadding(padX, padY, padX, padY / 2);

                    var editText = new EditText(activity)
                    {
                        Text = currentValue,
                        Hint = hint,
                        InputType = isPassword
                            ? (InputTypes.ClassText | InputTypes.TextVariationPassword)
                            : (InputTypes.ClassText | InputTypes.TextFlagNoSuggestions)
                    };
                    editText.SetTextColor(Android.Graphics.Color.White);
                    editText.SetHintTextColor(Android.Graphics.Color.Rgb(165, 175, 190));
                    editText.SetBackgroundColor(Android.Graphics.Color.Rgb(30, 35, 46));
                    editText.SetPadding(innerPad, innerPad, innerPad, innerPad);
                    editText.SetSingleLine(true);
                    editText.SetSelection(editText.Text?.Length ?? 0);
                    container.AddView(editText, new FrameLayout.LayoutParams(
                        ViewGroup.LayoutParams.MatchParent,
                        ViewGroup.LayoutParams.WrapContent));

                    var builder = new AlertDialog.Builder(activity, Android.Resource.Style.ThemeDeviceDefaultDialogAlert)
                        .SetTitle(title)
                        .SetView(container)
                        .SetCancelable(true)
                        .SetPositiveButton("确定", (_, _) =>
                        {
                            string entered = editText.Text ?? string.Empty;
                            MuGame.ScheduleOnMainThread(() =>
                            {
                                Value = entered;
                                OnBlur();
                            });
                        })
                        .SetNegativeButton("取消", (_, _) =>
                        {
                            MuGame.ScheduleOnMainThread(OnBlur);
                        });

                    var dialog = builder.Create()!;
                    dialog.DismissEvent += (_, _) => Interlocked.Exchange(ref _dialogOpen, 0);
                    dialog.Window?.SetSoftInputMode(SoftInput.StateAlwaysVisible);
                    dialog.Show();

                    editText.RequestFocus();
                    editText.PostDelayed(() =>
                    {
                        var imm = activity.GetSystemService(Android.Content.Context.InputMethodService) as InputMethodManager;
                        imm?.ShowSoftInput(editText, ShowFlags.Implicit);
                    }, 80);
                }
                catch
                {
                    Interlocked.Exchange(ref _dialogOpen, 0);
                }
            });
        }
    }
}
