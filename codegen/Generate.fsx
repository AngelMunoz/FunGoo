// Generates the FunGoo F# helper surface from the Goo public API.
// Plan: docs/codegen-plan.md. Run: dotnet fsi codegen/Generate.fsx
// Precondition: Goo is built (Goo/Goo/bin/Debug/net10.0/Goo.dll exists).
// The reflection scans below are the single source of truth: new Goo setters,
// children-bearing widgets, Try methods and implicit operators flow through
// on the next run. Generated files overwrite idempotently.
#r "nuget: Fabulous.AST, 2.0.0-pre08"
#r "nuget: Hexa.NET.SDL3, 1.2.17"
#r "nuget: HexaGen.Runtime, 1.1.24"
#r "nuget: Unicode.Bidi, 0.3.18"

#I __SOURCE_DIRECTORY__
#r "../Goo/Goo/bin/Debug/net10.0/Yoga.Net.dll"
#r "../Goo/Goo/bin/Debug/net10.0/Goo.dll"

open System
open System.IO
open System.Reflection
open Fabulous.AST
open type Fabulous.AST.Ast

module SyntaxOak = Fantomas.Core.SyntaxOak

open Fantomas.FCS.Text

open type Goo.Blob

// ---------------------------------------------------------------------------
// Reflection scans
// ---------------------------------------------------------------------------

let gooAsm = typeof<Goo.Container>.Assembly

let exportedTypes =
  try
    gooAsm.GetExportedTypes() |> Array.filter(fun t -> not(isNull(box t)))
  with :? ReflectionTypeLoadException as e ->
    e.Types |> Array.filter(fun t -> not(isNull(box t)))

let instanceFlags =
  BindingFlags.Public ||| BindingFlags.Instance ||| BindingFlags.DeclaredOnly

let allFlags =
  BindingFlags.Public
  ||| BindingFlags.Static
  ||| BindingFlags.Instance
  ||| BindingFlags.DeclaredOnly

let isInitOnly(methodInfo: MethodInfo) =
  methodInfo.ReturnParameter.GetRequiredCustomModifiers()
  |> Array.exists(fun m ->
    m.Name = "IsExternalInit"
    || m.FullName = "System.Runtime.CompilerServices.IsExternalInit")

/// Types with a public instance Children : IList<Blob> property.
let childrenTypes =
  exportedTypes
  |> Array.filter(fun t ->
    t.GetProperties(instanceFlags)
    |> Array.exists(fun p ->
      p.Name = "Children"
      && p.PropertyType.IsGenericType
      && p.PropertyType.GetGenericTypeDefinition().Name = "IList`1"
      && p.PropertyType.GetGenericArguments()[0] = typeof<Goo.Blob>))
  |> Array.sortBy(fun t -> t.Name)

/// Public instance properties with a real (non-init-only) setter, grouped by type.
let setterGroups =
  exportedTypes
  |> Array.choose(fun t ->
    let props =
      t.GetProperties(instanceFlags)
      |> Array.filter(fun p ->
        match p.GetSetMethod() with
        | null -> false
        | setter -> not(isInitOnly setter))
      |> Array.sortBy(fun p -> p.Name)

    if props.Length = 0 then None else Some(t, props))
  |> Array.sortBy(fun (t, _) -> t.Name)

/// Public Try* methods with exactly one out parameter, excluding Deconstruct-style
/// compiler sugar. Returns (type, method, outParameter).
let tryMethods =
  exportedTypes
  |> Array.collect(fun t ->
    t.GetMethods(allFlags)
    |> Array.choose(fun m ->
      if m.IsSpecialName || not(m.Name.StartsWith("Try")) then
        None
      else
        let outs = m.GetParameters() |> Array.filter(fun p -> p.IsOut)

        match outs with
        | [| out |] -> Some(t, m, out)
        | _ -> None))
  |> Array.sortBy(fun (t, m, _) -> t.Name, m.Name)

/// Implicit conversion operators across the surface: (fromType, toType).
let implicitOperators =
  exportedTypes
  |> Array.collect(fun t ->
    t.GetMethods(allFlags)
    |> Array.choose(fun m ->
      if m.Name <> "op_Implicit" then
        None
      else
        match m.GetParameters() with
        | [| from |] -> Some(from.ParameterType, m.ReturnType)
        | _ -> None))
  |> Array.distinct
  |> Array.sortBy(fun (from, to_) -> to_.Name, from.Name)

// ---------------------------------------------------------------------------
// Type and identifier rendering
// ---------------------------------------------------------------------------

let camelCase(name: string) =
  if String.IsNullOrEmpty(name) || Char.IsLower name[0] then
    name
  else
    string(Char.ToLower name[0]) + name.Substring(1)

let rec fsType(t: Type) =
  if t = typeof<bool> then
    "bool"
  elif t = typeof<int> then
    "int"
  elif t = typeof<string> then
    "string"
  elif t = typeof<double> then
    "float"
  elif t = typeof<float32> then
    "float32"
  elif t = typeof<int64> then
    "int64"
  elif t = typeof<uint64> then
    "uint64"
  elif t.IsByRef then
    fsType(t.GetElementType())
  elif t.IsArray then
    (fsType(t.GetElementType())) + "[]"
  elif t.IsGenericType then
    let definition = t.GetGenericTypeDefinition()
    let name = definition.Name.Split('`')[0]

    let ns =
      if
        definition.Namespace = "System"
        || String.IsNullOrEmpty(definition.Namespace)
      then
        ""
      else
        definition.Namespace + "."

    let args = t.GetGenericArguments() |> Array.map fsType |> String.concat ", "
    $"{ns}{name}<{args}>"
  else
    t.FullName

// ---------------------------------------------------------------------------
// Emission: Fabulous.AST widgets
// ---------------------------------------------------------------------------

let header moduleName (nowarnCodes: string list) =
  let nowarnLines =
    nowarnCodes
    |> List.map(fun code -> $"#nowarn \"%s{code}\"")
    |> String.concat "\n"

  let nowarnBlock = if nowarnCodes.IsEmpty then "" else $"\n%s{nowarnLines}\n"

  $"// GENERATED — DO NOT EDIT\n// %s{moduleName} generated by codegen/Generate.fsx from the Goo public surface.\n%s{nowarnBlock}"

let widgetsDir =
  Path.Combine(__SOURCE_DIRECTORY__, "..", "FunGoo", "Widgets")
  |> Path.GetFullPath

let writeFile fileName (content: string) =
  File.WriteAllText(Path.Combine(widgetsDir, fileName), content)
  printfn $"wrote %s{fileName} (%d{content.Length} chars)"

/// Signature-type widget for a .fsi member slot or val.
let rec fsiType(t: Type) : WidgetBuilder<SyntaxOak.Type> =
  if t = typeof<bool> then
    LongIdent("bool")
  elif t = typeof<int> then
    LongIdent("int")
  elif t = typeof<string> then
    LongIdent("string")
  elif t = typeof<double> then
    LongIdent("float")
  elif t = typeof<float32> then
    LongIdent("float32")
  elif t = typeof<int64> then
    LongIdent("int64")
  elif t = typeof<uint64> then
    LongIdent("uint64")
  elif t.IsArray then
    Array(fsiType(t.GetElementType()))
  elif t.IsGenericType then
    let definition = t.GetGenericTypeDefinition()
    let name = definition.Name.Split('`')[0]

    let ns =
      if
        definition.Namespace = "System"
        || String.IsNullOrEmpty(definition.Namespace)
      then
        ""
      else
        definition.Namespace + "."

    AppPrefix($"{ns}{name}", fsType(t.GetGenericArguments()[0]))
  else
    LongIdent(t.FullName)

/// Signature member slot: `[<Extension>] static member inline Name: params -> ret`.
/// Built from a raw ValNode because the SignatureParameter attribute (ParamArray)
/// and the member-level Extension attribute are not exposed by the Val widget.
let fsiMemberSlot
  (name: string)
  (parameters: WidgetBuilder<SyntaxOak.Type> list)
  (returnType: WidgetBuilder<SyntaxOak.Type>)
  =
  let tn(s: string) = SyntaxOak.SingleTextNode(s, Range.Zero)

  let attributeNode = Attribute("Extension") |> Gen.mkOak

  let attributeList =
    SyntaxOak.AttributeListNode(tn "[<", [ attributeNode ], tn ">]", Range.Zero)

  let attributes =
    SyntaxOak.MultipleAttributeListNode([ attributeList ], Range.Zero)

  let leadingKeywords =
    SyntaxOak.MultipleTextsNode([ tn "static"; tn "member" ], Range.Zero)

  let inlineNode = SyntaxOak.SingleTextNode("inline", Range.Zero)

  let valNode =
    SyntaxOak.ValNode(
      None,
      Some attributes,
      Some leadingKeywords,
      Some inlineNode,
      false,
      None,
      tn name,
      None,
      Gen.mkOak(Funs([ Tuple(parameters) ], returnType)),
      None,
      None,
      Range.Zero
    )

  SigMember(EscapeHatch(valNode))

let renderFsiFile
  moduleName
  (types: WidgetBuilder<SyntaxOak.TypeDefn> seq)
  (valueSlots: WidgetBuilder<SyntaxOak.ValNode> seq)
  =
  let code =
    Oak() {
      Namespace("FunGoo") {
        Module(moduleName) {
          Open("System")
          Open("System.Runtime.CompilerServices")

          yield!
            (types
             |> Seq.map(fun t ->
               Ast.EscapeHatch(
                 SyntaxOak.ModuleDecl.TypeDefn(Gen.mkOak t)
                 : SyntaxOak.ModuleDecl
               )))

          yield!
            (valueSlots
             |> Seq.map(fun v ->
               Ast.EscapeHatch(
                 SyntaxOak.ModuleDecl.Val(Gen.mkOak v): SyntaxOak.ModuleDecl
               )))
        }
      }
    }
    |> Gen.mkOak
    |> Gen.run

  let content = header moduleName [] + code

  // A signature file must parse in signature mode; fail loudly otherwise.
  let diagnostics =
    try
      Async.RunSynchronously(
        Fantomas.Core.CodeFormatter.ParseOakAsync(true, content)
      )[0]
      |> snd
    with :? Fantomas.Core.ParseException as e ->
      File.WriteAllText(
        Path.Combine(widgetsDir, $"{moduleName}.invalid.fsi"),
        content
      )

      failwith
        $"generated %s{moduleName}.g.fsi does not parse as a signature file: %s{e.Message}"

  if not diagnostics.IsEmpty then
    for d in diagnostics do
      eprintfn $"%s{moduleName}.g.fsi: %s{d}"

    File.WriteAllText(
      Path.Combine(widgetsDir, $"{moduleName}.invalid.fsi"),
      content
    )

    failwith $"generated %s{moduleName}.g.fsi is not a valid signature file"

  File.WriteAllText(Path.Combine(widgetsDir, $"{moduleName}.g.fsi"), content)
  printfn $"wrote %s{moduleName}.g.fsi"

let extensionClass
  (name: string)
  (memberSpecs: 'a seq)
  (buildMember: 'a -> WidgetBuilder<SyntaxOak.MemberDefn>)
  =
  TypeDefn(name) {
    for spec in memberSpecs do
      buildMember spec
  }
  |> _.attributes([ Attribute("Extension"); Attribute("Sealed") ])



// ---------------------------------------------------------------------------
// Children.g.fs — seq and ParamArray members, appending through IList
// ---------------------------------------------------------------------------

let childrenMember
  (t: Type)
  (attribute: string voption)
  (parameterType: string)
  =
  let childrenParameter =
    let parameter = ParameterPat("children", LongIdent(parameterType))

    match attribute with
    | ValueSome name -> parameter |> _.attributes([ Attribute(name) ])
    | ValueNone -> parameter

  Member(
    "Children",
    [
      ParenPat(
        TuplePat(
          [
            ParameterPat("this", LongIdent($"Goo.{t.Name}"))
            childrenParameter
          ]
        )
      )
    ],
    CompExprBodyExpr [
      OtherExpr(
        ForEachDoExpr(
          "v",
          "children",
          AppExpr("this.Children.Add", [ ConstantExpr("v") ])
        )
      )
      OtherExpr("this")
    ],
    $"Goo.{t.Name}"
  )
  |> _.attribute(Attribute("Extension"))
  |> _.toStatic()
  |> _.toInlined()

let childrenType(t: Type) =
  extensionClass
    $"{t.Name}Extensions"
    [ ValueNone, "seq<Goo.Blob>"; ValueSome "ParamArray", "Goo.Blob[]" ]
    (fun (attribute, parameterType) -> childrenMember t attribute parameterType)

let childrenFsiType(t: Type) =
  let slot
    (parameterType: WidgetBuilder<SyntaxOak.Type>)
    (paramAttribute: string voption)
    =
    let parameter =
      match paramAttribute with
      | ValueSome name ->
        (SignatureParameter("children", parameterType))
        |> _.attribute(Attribute(name))
      | ValueNone -> SignatureParameter("children", parameterType)

    fsiMemberSlot
      "Children"
      [ SignatureParameter("this", LongIdent($"Goo.{t.Name}")); parameter ]
      (LongIdent($"Goo.{t.Name}"))

  TypeDefn($"{t.Name}Extensions") {
    slot (AppPrefix("seq", "Goo.Blob")) ValueNone
    slot (Array(LongIdent("Goo.Blob"))) (ValueSome "ParamArray")
  }
  |> _.attributes([ Attribute("Extension"); Attribute("Sealed") ])

// ---------------------------------------------------------------------------
// Setters.g.fs — fluent setters on the mutable Goo surface
// ---------------------------------------------------------------------------

let setterMember(t: Type, p: PropertyInfo) =
  Member(
    p.Name,
    [
      ParenPat(
        TuplePat(
          [
            ParameterPat("this", LongIdent($"Goo.{t.Name}"))
            ParameterPat(camelCase p.Name, LongIdent(fsType p.PropertyType))
          ]
        )
      )
    ],
    CompExprBodyExpr [
      OtherExpr(SetExpr($"this.{p.Name}", camelCase p.Name))
      OtherExpr("this")
    ],
    $"Goo.{t.Name}"
  )
  |> _.attribute(Attribute("Extension"))
  |> _.toStatic()
  |> _.toInlined()

let setterType(t: Type, props: PropertyInfo[]) =
  extensionClass $"{t.Name}Extensions" props (fun p -> setterMember(t, p))

let setterFsiType(t: Type, props: PropertyInfo[]) =
  TypeDefn($"{t.Name}Extensions") {
    for p in props do
      fsiMemberSlot
        p.Name
        [
          SignatureParameter("this", LongIdent($"Goo.{t.Name}"))
          SignatureParameter(camelCase p.Name, fsiType p.PropertyType)
        ]
        (LongIdent($"Goo.{t.Name}"))
  }
  |> _.attributes([ Attribute("Extension"); Attribute("Sealed") ])

// ---------------------------------------------------------------------------
// Interop.g.fs — voption wrappers for Try* out parameters and implicit helpers
// ---------------------------------------------------------------------------

let tryMember(t: Type, m: MethodInfo, out: ParameterInfo) =
  let normal = m.GetParameters() |> Array.filter(fun p -> not p.IsOut)
  let outType = fsType(out.ParameterType.GetElementType())

  let callArguments =
    m.GetParameters()
    |> Array.map(fun p ->
      if p.IsOut then
        PrefixAppExpr("&", "value")
      else
        ConstantExpr(p.Name))
    |> Array.toList

  Member(
    m.Name,
    [
      ParenPat(
        TuplePat [
          ParameterPat("this", LongIdent $"Goo.{t.Name}")

          for p in normal do
            ParameterPat(p.Name, LongIdent(fsType p.ParameterType))

        ]
      )
    ],
    CompExprBodyExpr [
      OtherExpr $"let mutable value = Unchecked.defaultof<{outType}>"

      OtherExpr(
        IfThenElseExpr(
          AppExpr($"this.{m.Name}", [ ParenExpr(TupleExpr callArguments) ]),
          "ValueSome value",
          "ValueNone"
        )
      )
    ],
    $"{outType} voption"
  )
  |> _.attribute(Attribute("Extension"))
  |> _.toStatic()
  |> _.toInlined()

let implicitHelper(fromType: Type, toType: Type) =
  let sourceName =
    fromType.Name.Replace("Double", "Float").Replace("Int32", "Int")

  let targetName =
    camelCase(toType.Name.Replace("Double", "Float").Replace("Int32", "Int"))

  // The annotated identity body relies on F# type-directed op_Implicit
  // insertion, which must be validated per pair by the smoke check. Pairs
  // that fail it get an explicit parse call instead.
  let body =
    if fromType = typeof<string> && toType = typeof<Goo.Color> then
      AppExpr("Goo.Color.Parse", [ ConstantExpr("value") ])
    else
      ConstantExpr("value")

  Function(
    $"{targetName}From{sourceName}",
    [ ParenPat(ParameterPat("value", LongIdent(fsType fromType))) ],
    body,
    fsType toType
  )
  |> _.toInlined()

let renderInterop() =
  let code =
    Oak() {
      Namespace("FunGoo") {
        Module("Interop") {
          Open("System")
          Open("System.Runtime.CompilerServices")

          extensionClass "InteropExtensions" tryMethods (fun (t, m, out) ->
            tryMember(t, m, out))

          for pair in implicitOperators do
            implicitHelper pair
        }
      }
    }
    |> Gen.mkOak
    |> Gen.run

  header "Interop" [ "3391" ] + code

let tryFsiSlot(t: Type, m: MethodInfo, _) =
  let normal = m.GetParameters() |> Array.filter(fun p -> not p.IsOut)

  let outParameter = m.GetParameters() |> Array.find(fun p -> p.IsOut)

  let outType = fsiType(outParameter.ParameterType.GetElementType())

  fsiMemberSlot
    m.Name
    [
      yield SignatureParameter("this", LongIdent($"Goo.{t.Name}"))

      for p in normal do
        yield SignatureParameter(p.Name, fsiType p.ParameterType)
    ]
    (AppPostfix(outType, "voption"))

let implicitHelperVal(fromType: Type, toType: Type) =
  let sourceName =
    fromType.Name.Replace("Double", "Float").Replace("Int32", "Int")

  let targetName =
    camelCase(toType.Name.Replace("Double", "Float").Replace("Int32", "Int"))

  Val(
    [ "val"; "inline" ],
    $"{targetName}From{sourceName}",
    Funs([ SignatureParameter("value", fsiType fromType) ], fsiType toType)
  )

let interopFsiTypes() =
  let tryExtension =
    TypeDefn("InteropExtensions") {
      for (t, m, out) in tryMethods do
        tryFsiSlot(t, m, out)
    }
    |> _.attributes([ Attribute("Extension"); Attribute("Sealed") ])

  tryExtension

// ---------------------------------------------------------------------------
// Run
// ---------------------------------------------------------------------------

printfn
  "scans: %d children types, %d setter types (%d setters), %d try methods, %d implicits"
  childrenTypes.Length
  setterGroups.Length
  (setterGroups |> Array.sumBy(fun (_, p) -> p.Length))
  tryMethods.Length
  implicitOperators.Length

Directory.CreateDirectory(widgetsDir) |> ignore

let childrenCode =
  Oak() {
    Namespace("FunGoo") {
      Module("Children") {
        Open("System")
        Open("System.Runtime.CompilerServices")

        for t in childrenTypes do
          childrenType t
      }
    }
  }
  |> Gen.mkOak
  |> Gen.run

writeFile "Children.g.fs" (header "Children" [] + childrenCode)

renderFsiFile
  "Children"
  [ for t in childrenTypes -> childrenFsiType t ]
  Seq.empty

let settersCode =
  Oak() {
    Namespace("FunGoo") {
      Module("Setters") {
        Open("System")
        Open("System.Runtime.CompilerServices")

        for group in setterGroups do
          setterType group
      }
    }
  }
  |> Gen.mkOak
  |> Gen.run

writeFile "Setters.g.fs" (header "Setters" [] + settersCode)

renderFsiFile
  "Setters"
  [ for group in setterGroups -> setterFsiType group ]
  Seq.empty

writeFile "Interop.g.fs" (header "Interop" [ "3391" ] + renderInterop())

renderFsiFile
  "Interop"
  [ interopFsiTypes() ]
  (implicitOperators |> Seq.map implicitHelperVal)

printfn "done"
