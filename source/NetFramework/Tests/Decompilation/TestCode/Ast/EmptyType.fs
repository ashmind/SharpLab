type Empty = class end

(* ast

[
  {
    "kind": "ParsedImplFileInput",
    "type": "node",
    "children": [
      {
        "kind": "SynModuleOrNamespace",
        "type": "node",
        "range": "0-22",
        "children": [
          {
            "type": "token",
            "kind": "Ident",
            "property": "longId",
            "value": "_",
            "range": "0-0"
          },
          {
            "kind": "SynModuleDecl.Types",
            "type": "node",
            "range": "0-22",
            "children": [
              {
                "kind": "SynTypeDefn",
                "type": "node",
                "range": "5-22",
                "children": [
                  {
                    "kind": "SynComponentInfo",
                    "property": "typeInfo",
                    "type": "node",
                    "range": "5-10",
                    "children": [
                      {
                        "type": "token",
                        "kind": "Ident",
                        "property": "longId",
                        "value": "Empty",
                        "range": "5-10"
                      }
                    ]
                  },
                  {
                    "kind": "SynTypeDefnRepr.ObjectModel",
                    "property": "typeRepr",
                    "type": "node",
                    "range": "13-22",
                    "children": [
                      {
                        "kind": "SynTypeDefnKind",
                        "property": "kind",
                        "type": "node",
                        "value": "Class"
                      }
                    ]
                  }
                ]
              }
            ]
          }
        ]
      },
      {
        "kind": "SynModuleOrNamespace",
        "type": "node",
        "range": "0-22",
        "children": [
          {
            "type": "token",
            "kind": "Ident",
            "property": "longId",
            "value": "_",
            "range": "0-0"
          },
          {
            "kind": "SynModuleDecl.Types",
            "type": "node",
            "range": "0-22",
            "children": [
              {
                "kind": "SynTypeDefn",
                "type": "node",
                "range": "5-22",
                "children": [
                  {
                    "kind": "SynComponentInfo",
                    "property": "typeInfo",
                    "type": "node",
                    "range": "5-10",
                    "children": [
                      {
                        "type": "token",
                        "kind": "Ident",
                        "property": "longId",
                        "value": "Empty",
                        "range": "5-10"
                      }
                    ]
                  },
                  {
                    "kind": "SynTypeDefnRepr.ObjectModel",
                    "property": "typeRepr",
                    "type": "node",
                    "range": "13-22",
                    "children": [
                      {
                        "kind": "SynTypeDefnKind",
                        "property": "kind",
                        "type": "node",
                        "value": "Class"
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