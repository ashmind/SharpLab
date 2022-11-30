/// <summary>Test</summary>

/* ast

[
  {
    "type": "node",
    "kind": "CompilationUnit",
    "range": "0-27",
    "children": [
      {
        "type": "token",
        "kind": "EndOfFileToken",
        "property": "EndOfFileToken",
        "range": "0-27",
        "children": [
          {
            "type": "trivia",
            "kind": "SingleLineDocumentationCommentTrivia",
            "range": "0-27",
            "children": [
              {
                "type": "node",
                "kind": "SingleLineDocumentationCommentTrivia",
                "property": "Structure",
                "range": "0-27",
                "children": [
                  {
                    "type": "node",
                    "kind": "XmlText",
                    "range": "0-4",
                    "children": [
                      {
                        "type": "token",
                        "kind": "XmlTextLiteralToken",
                        "range": "0-4",
                        "children": [
                          {
                            "type": "trivia",
                            "kind": "DocumentationCommentExteriorTrivia",
                            "range": "0-3",
                            "value": "///"
                          },
                          {
                            "type": "value",
                            "value": " ",
                            "range": "3-4"
                          }
                        ]
                      }
                    ]
                  },
                  {
                    "type": "node",
                    "kind": "XmlElement",
                    "range": "4-27",
                    "children": [
                      {
                        "type": "node",
                        "kind": "XmlElementStartTag",
                        "property": "StartTag",
                        "range": "4-13",
                        "children": [
                          {
                            "type": "token",
                            "kind": "LessThanToken",
                            "property": "LessThanToken",
                            "range": "4-5",
                            "value": "<"
                          },
                          {
                            "type": "node",
                            "kind": "XmlName",
                            "property": "Name",
                            "range": "5-12",
                            "children": [
                              {
                                "type": "token",
                                "kind": "IdentifierToken",
                                "property": "LocalName",
                                "range": "5-12",
                                "value": "summary"
                              }
                            ]
                          },
                          {
                            "type": "token",
                            "kind": "GreaterThanToken",
                            "property": "GreaterThanToken",
                            "range": "12-13",
                            "value": ">"
                          }
                        ]
                      },
                      {
                        "type": "node",
                        "kind": "XmlText",
                        "range": "13-17",
                        "children": [
                          {
                            "type": "token",
                            "kind": "XmlTextLiteralToken",
                            "range": "13-17",
                            "value": "Test"
                          }
                        ]
                      },
                      {
                        "type": "node",
                        "kind": "XmlElementEndTag",
                        "property": "EndTag",
                        "range": "17-27",
                        "children": [
                          {
                            "type": "token",
                            "kind": "LessThanSlashToken",
                            "property": "LessThanSlashToken",
                            "range": "17-19",
                            "value": "</"
                          },
                          {
                            "type": "node",
                            "kind": "XmlName",
                            "property": "Name",
                            "range": "19-26",
                            "children": [
                              {
                                "type": "token",
                                "kind": "IdentifierToken",
                                "property": "LocalName",
                                "range": "19-26",
                                "value": "summary"
                              }
                            ]
                          },
                          {
                            "type": "token",
                            "kind": "GreaterThanToken",
                            "property": "GreaterThanToken",
                            "range": "26-27",
                            "value": ">"
                          }
                        ]
                      }
                    ]
                  },
                  {
                    "type": "token",
                    "kind": "EndOfDocumentationCommentToken",
                    "property": "EndOfComment",
                    "range": "27-27",
                    "value": ""
                  }
                ]
              }
            ]
          },
          {
            "type": "value",
            "value": "",
            "range": "27-27"
          }
        ]
      }
    ]
  }
]

*/