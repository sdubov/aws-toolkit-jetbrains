using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.Application.UI.Icons.ComposedIcons;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features.RunMarkers;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;
using JetBrains.UI.ThemedIcons;
using JetBrains.Util;
using JetBrains.Util.Logging;
using ReSharper.AWS.Lambda;

namespace ReSharper.AWS.RunMarkers
{
  public abstract class LambdaRunMarkerGutterMark : IconGutterMark
  {
    private static readonly ILogger ourLogger = Logger.GetLogger<LambdaRunMarkerGutterMark>();

    public override IAnchor Anchor => BulbMenuAnchors.PermanentBackgroundItems;

    protected LambdaRunMarkerGutterMark([NotNull] IconId iconId) : base(iconId)
    {
    }

    public override IEnumerable<BulbMenuItem> GetBulbMenuItems(IHighlighter highlighter)
    {
      if (!(highlighter.UserData is RunMarkerHighlighting runMarker)) yield break;

      var solution = Shell.Instance.GetComponent<SolutionsManager>().Solution;
      if (solution == null) yield break;

      switch (runMarker.AttributeId)
      {
        case LambdaRunMarkerAttributeIds.LAMBDA_RUN_METHOD_MARKER_ID:
          foreach (var item in GetRunMethodItems(solution, runMarker))
          {
            yield return item;
          }
          yield break;
        default:
          yield break;
      }
    }

    private IEnumerable<BulbMenuItem> GetRunMethodItems(ISolution solution, RunMarkerHighlighting runMarker)
    {
      var lambdaHost = solution.GetComponent<LambdaHost>();

      var methodName = runMarker.Method.ShortName;
      var handlerString = ComposeHandlerString(runMarker);

      yield return new BulbMenuItem(
        new ExecutableItem(() => { lambdaHost.RunLambda(methodName, handlerString); }), new RichText($"Run '[Local] {methodName}'"),
        LambdaRunMarkersThemedIcons.RunThis.Id, BulbMenuAnchors.PermanentBackgroundItems);

      yield return new BulbMenuItem(
        new ExecutableItem(() => { lambdaHost.DebugLambda(methodName, handlerString); }), new RichText($"Debug '[Local] {methodName}'"),
        LambdaRunMarkersThemedIcons.DebugThis.Id, BulbMenuAnchors.PermanentBackgroundItems);

      yield return new BulbMenuItem(
        new ExecutableItem(() => { lambdaHost.CreateNewLambda(methodName, handlerString); }), new RichText("Create new AWS Lambda..."),
        CompositeIconId.Compose(LambdaRunMarkersThemedIcons.Lambda.Id, LambdaRunMarkersThemedIcons.CreateNew.Id), BulbMenuAnchors.PermanentBackgroundItems);
    }

    private string ComposeHandlerString(RunMarkerHighlighting runMarker)
    {
      var assemblyName = runMarker.Project.GetOutputAssemblyName(runMarker.TargetFrameworkId);
      var methodName = runMarker.Method.ShortName;
      var typeString = runMarker.FullName.Substring(0, runMarker.FullName.Length - methodName.Length - 1);

      return $"{assemblyName}::{typeString}::{methodName}";
    }
  }

  public class LambdaMethodRunMarkerGutterMark : LambdaRunMarkerGutterMark
  {
    public LambdaMethodRunMarkerGutterMark() : base(LambdaRunMarkersThemedIcons.Lambda.Id)
    {
    }
  }
}