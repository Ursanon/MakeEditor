﻿using UnityEditor;

namespace Bloodstone.MakeEditor
{
    public class EditorDialog
    {
        public readonly string Title;
        public readonly string AcceptText;
        public readonly string DeclineText;

        public EditorDialog(string title, string acceptText, string declineText)
        {
            Title = title;
            AcceptText = acceptText;
            DeclineText = declineText;
        }

        public Option Show(string message)
        {
            var result = EditorUtility.DisplayDialog(Title, message, AcceptText, DeclineText);
            return result ? Option.Accepted : Option.Declined;
        }

        public enum Option { Declined, Accepted };
    }
}