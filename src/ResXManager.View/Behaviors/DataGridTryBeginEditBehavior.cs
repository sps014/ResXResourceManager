namespace ResXManager.View.Behaviors
{
    using System.Linq;
    using System.Windows.Controls;
    using System.Windows.Data;
    using Microsoft.Xaml.Behaviors;

    using ResXManager.Model;
    using ResXManager.View.ColumnHeaders;

    public class DataGridTryBeginEditBehavior : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.BeginningEdit += DataGrid_BeginningEdit;
            AssociatedObject.CellEditEnding += AssociatedObject_CurrentCellChanged;
        }
        private static string previousValueSelectedCell=string.Empty;
        private void AssociatedObject_CurrentCellChanged(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit)
                return;

            //peform analysis here
            var bindingGroup = e.EditingElement.BindingGroup.BindingExpressions.FirstOrDefault();
            if (bindingGroup == null)
                return;

            //bindingExpression
            var expr = (BindingExpression)bindingGroup;
            var resourceEntry = expr.DataItem as ResourceTableEntry;

            var columnName = expr.ResolvedSourcePropertyName;
            if (columnName == "Key")
                return;

            OnEditEnded?.Invoke(sender, new CustomEditCommitArgs()
            {
                ColumnName = columnName,
                CurrentValue = (expr.Target as TextBox).Text,
                Entry = resourceEntry,
                PreviousValue = previousValueSelectedCell
            });
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.BeginningEdit -= DataGrid_BeginningEdit;
            AssociatedObject.CellEditEnding -= AssociatedObject_CurrentCellChanged;
        }

        private static void DataGrid_BeginningEdit(object? sender, DataGridBeginningEditEventArgs e)
        {
            var dataGridRow = e.Row;
            var entry = (ResourceTableEntry)dataGridRow.Item;
            var resourceEntity = entry.Container;

            var resourceLanguages = resourceEntity.Languages;
            if (!resourceLanguages.Any())
                return;

            var cultureKey = resourceLanguages.First()?.CultureKey;

            if (e.Column.Header is ILanguageColumnHeader languageHeader)
            {
                cultureKey = languageHeader.CultureKey;
            }

            if (!resourceEntity.CanEdit(cultureKey))
            {
                e.Cancel = true;
            }

            var textBlock = e.EditingEventArgs.OriginalSource as TextBlock;
            if (textBlock != null)
                previousValueSelectedCell = textBlock.Text;
            else
                previousValueSelectedCell = string.Empty;

            
        }
        public delegate void OnEditCommitEndedHandler(object? sender,CustomEditCommitArgs e);
        public static event OnEditCommitEndedHandler? OnEditEnded;
          
    }
    public class CustomEditCommitArgs
    {
        public string ColumnName { get; set; }
        public ResourceTableEntry Entry { get; set; }
        public string PreviousValue { get; set; }
        public string CurrentValue { get; set; }
        public string? Culture { get; internal set; }
    }

}
