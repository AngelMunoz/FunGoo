namespace FunGoo.Metro

open System
open System.Collections.Generic
open Goo
open Goo.Widgets.Icons
open Goo.Widgets.Navigation

[<Struct; NoComparison>]
type MetroBreadcrumbItem = {
  Id: string
  Text: string
  IconName: string voption
}

[<Struct; NoComparison>]
type MetroBreadcrumbProps = {
  Items: MetroBreadcrumbItem array
  KeepStart: int
  KeepEnd: int
  AccessibilityName: string voption
}

type IMetroBreadcrumbFactory =
  abstract CreateInput: props: MetroBreadcrumbProps -> BreadcrumbInput
  abstract Mount: key: string * props: MetroBreadcrumbProps -> Blob

module BreadcrumbInternals =

  let newBreadcrumb() : Breadcrumb = new Breadcrumb()

  let mapItems
    (source: MetroBreadcrumbItem array, iconsById: Dictionary<string, string>)
    : BreadcrumbItem array =
    iconsById.Clear()

    if isNull source || source.Length = 0 then
      Array.empty
    else
      let mapped = Array.zeroCreate<BreadcrumbItem> source.Length

      for i = 0 to source.Length - 1 do
        let seg = source[i]

        seg.IconName
        |> ValueOption.iter(fun name ->
          if
            not(String.IsNullOrWhiteSpace name)
            && not(String.IsNullOrEmpty seg.Id)
          then
            iconsById[seg.Id] <- name)

        mapped[i] <- BreadcrumbItem(Id = seg.Id, Label = seg.Text)

      mapped

[<RequireQualifiedAccess>]
module MetroBreadcrumbBar =

  let item(id: string, label: string) : MetroBreadcrumbItem = {
    Id = id
    Text = label
    IconName = ValueNone
  }

  let iconItem
    (id: string, label: string, iconName: string)
    : MetroBreadcrumbItem = {
    Id = id
    Text = label
    IconName = ValueSome iconName
  }

  let defaults(items: MetroBreadcrumbItem array) : MetroBreadcrumbProps = {
    Items = items
    KeepStart = 1
    KeepEnd = 2
    AccessibilityName = ValueNone
  }

  type CacheState = {
    IconsById: Dictionary<string, string>
    mutable SourceItems: MetroBreadcrumbItem array
    mutable MappedItems: BreadcrumbItem array
  }

  let createFactory(onSelect: string -> unit) : IMetroBreadcrumbFactory =
    let cache: CacheState = {
      IconsById = Dictionary<string, string>(StringComparer.Ordinal)
      SourceItems = null
      MappedItems = Array.empty
    }

    let selectDelegate = Action<string>(onSelect)

    // Builds fresh icon Blobs inside Breadcrumb.Build so focus/theme rebuilds
    // never reuse a frozen Blob.
    let createItem =
      Func<BreadcrumbInput, BreadcrumbItem, Blob, Blob>(fun _ seg prepared ->
        if not(isNull seg.Id) && prepared :? Container then
          match cache.IconsById.TryGetValue seg.Id with
          | true, iconName ->
            let c = prepared :?> Container
            c.Children.Insert(0, MaterialIcons.Create(iconName, 16.0))
          | false, _ -> ()

        prepared)

    let createInput(props: MetroBreadcrumbProps) : BreadcrumbInput =
      if not(Object.ReferenceEquals(cache.SourceItems, props.Items)) then
        cache.SourceItems <- props.Items

        cache.MappedItems <-
          BreadcrumbInternals.mapItems(props.Items, cache.IconsById)

      let a11yName = props.AccessibilityName |> ValueOption.toObj

      BreadcrumbInput(
        Items = cache.MappedItems,
        OnSelect = selectDelegate,
        KeepStart = max 1 props.KeepStart,
        KeepEnd = max 1 props.KeepEnd,
        AccessibilityName = a11yName,
        CreateItem = createItem
      )

    {
      new IMetroBreadcrumbFactory with
        member _.CreateInput(props) = createInput props

        member _.Mount(key, props) =
          let input = createInput props

          Cell.Mount<BreadcrumbInput, Breadcrumb>(
            key,
            input,
            BreadcrumbInternals.newBreadcrumb
          )
    }
