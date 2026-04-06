using UnityEngine;
using UnityEngine.UIElements;

namespace LiberateUI
{
    public class KeyBindingController
    {
        private const string keyBindingClassName = "key-binding";
        private const string keyBindingHighlightedClassName = "key-binding--highlighted";

        private readonly VisualElement root;
        private Label currentlyListeningLabel;
        private bool isListeningForInput;

        public KeyBindingController(VisualElement root)
        {
            this.root = root;
        }

        public void RegisterKeyBindingCallbacks()
        {
            UQueryBuilder<Label> keyBindings = GetAllKeyBindings();
            keyBindings.ForEach(label => label.RegisterCallback<ClickEvent>(OnKeyBindingClick));
        }

        public void UnregisterKeyBindingCallbacks()
        {
            UQueryBuilder<Label> keyBindings = GetAllKeyBindings();
            keyBindings.ForEach(label => label.UnregisterCallback<ClickEvent>(OnKeyBindingClick));
        }

        private UQueryBuilder<Label> GetAllKeyBindings()
        {
            return root.Query<Label>(className: keyBindingClassName);
        }

        private void OnKeyBindingClick(ClickEvent evt)
        {
            Label clickedLabel = evt.currentTarget as Label;

            // If we're already listening to another label, unhighlight it
            if (currentlyListeningLabel != null && currentlyListeningLabel != clickedLabel)
            {
                currentlyListeningLabel.RemoveFromClassList(keyBindingHighlightedClassName);
            }

            // Unregister any existing keyboard listener first to avoid double registration
            if (isListeningForInput)
            {
                root.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            }

            // Highlight the clicked label
            clickedLabel.AddToClassList(keyBindingHighlightedClassName);
            currentlyListeningLabel = clickedLabel;
            isListeningForInput = true;

            // Register keyboard event listener
            root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (!isListeningForInput || currentlyListeningLabel == null)
            {
                return;
            }

            // Ignore modifier keys alone
            if (evt.keyCode == KeyCode.LeftShift || evt.keyCode == KeyCode.RightShift ||
                evt.keyCode == KeyCode.LeftControl || evt.keyCode == KeyCode.RightControl ||
                evt.keyCode == KeyCode.LeftAlt || evt.keyCode == KeyCode.RightAlt ||
                evt.keyCode == KeyCode.LeftCommand || evt.keyCode == KeyCode.RightCommand)
            {
                return;
            }

            string newKeyDisplayName = GetKeyDisplayName(evt.keyCode);

            // Check for duplicate key bindings
            Label conflictingLabel = FindKeyBindingWithText(newKeyDisplayName);

            if (conflictingLabel != null && conflictingLabel != currentlyListeningLabel)
            {
                // Set the conflicting label to "None"
                conflictingLabel.text = "None";
            }

            // Update the current label with the new key
            currentlyListeningLabel.text = newKeyDisplayName;

            // Remove highlight
            currentlyListeningLabel.RemoveFromClassList(keyBindingHighlightedClassName);

            // Stop listening
            isListeningForInput = false;
            currentlyListeningLabel = null;

            // Unregister keyboard event listener
            root.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

            // Prevent the event from propagating
            evt.StopPropagation();
        }

        private Label FindKeyBindingWithText(string keyText)
        {
            Label foundLabel = null;
            GetAllKeyBindings().ForEach(label =>
            {
                if (label.text == keyText)
                {
                    foundLabel = label;
                }
            });
            return foundLabel;
        }

        private string GetKeyDisplayName(KeyCode keyCode)
        {
            // Convert KeyCode to a more readable display name
            switch (keyCode)
            {
                case KeyCode.Space:
                    return "Space";
                case KeyCode.LeftShift:
                case KeyCode.RightShift:
                    return "Shift";
                case KeyCode.LeftControl:
                case KeyCode.RightControl:
                    return "Ctrl";
                case KeyCode.LeftAlt:
                case KeyCode.RightAlt:
                    return "Alt";
                default:
                    return keyCode.ToString();
            }
        }

        public void CancelListening()
        {
            if (currentlyListeningLabel != null)
            {
                currentlyListeningLabel.RemoveFromClassList(keyBindingHighlightedClassName);
                currentlyListeningLabel = null;
            }

            if (isListeningForInput)
            {
                root.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
                isListeningForInput = false;
            }
        }
    }
}
