1
'c'
"a
b"

(* ast

[
  {
    "kind": "ParsedImplFileInput",
    "type": "node",
    "children": [
      {
        "kind": "SynModuleOrNamespace",
        "type": "node",
        "range": "0-14",
        "children": [
          {
            "type": "token",
            "kind": "Ident",
            "property": "longId",
            "value": "_",
            "range": "0-0"
          },
          {
            "kind": "SynModuleDecl.DoExpr",
            "type": "node",
            "range": "0-1",
            "children": [
              {
                "kind": "DebugPointAtBinding.Yes",
                "property": "debugPoint",
                "type": "node"
              },
              {
                "kind": "SynExpr.Const",
                "property": "expr",
                "type": "node",
                "range": "0-1",
                "children": [
                  {
                    "kind": "SynConst.Int32",
                    "property": "constant",
                    "type": "token",
                    "value": "1"
                  }
                ]
              }
            ]
          },
          {
            "kind": "SynModuleDecl.DoExpr",
            "type": "node",
            "range": "3-6",
            "children": [
              {
                "kind": "DebugPointAtBinding.Yes",
                "property": "debugPoint",
                "type": "node"
              },
              {
                "kind": "SynExpr.Const",
                "property": "expr",
                "type": "node",
                "range": "3-6",
                "children": [
                  {
                    "kind": "SynConst.Char",
                    "property": "constant",
                    "type": "token",
                    "value": "'c'"
                  }
                ]
              }
            ]
          },
          {
            "kind": "SynModuleDecl.DoExpr",
            "type": "node",
            "range": "8-14",
            "children": [
              {
                "kind": "DebugPointAtBinding.Yes",
                "property": "debugPoint",
                "type": "node"
              },
              {
                "kind": "SynExpr.Const",
                "property": "expr",
                "type": "node",
                "range": "8-14",
                "children": [
                  {
                    "kind": "SynConst.String",
                    "property": "constant",
                    "type": "token",
                    "value": "\"a\r\nb\"",
                    "children": [
                      {
                        "kind": "SynStringKind",
                        "property": "synStringKind",
                        "type": "value",
                        "value": "Regular"
                      }
                    ]
                  }
                ]
              }
            ]
          }
        ]
      }
    ]
  }
]

*)