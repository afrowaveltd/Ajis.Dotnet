# Copilot Instructions

## Project Guidelines
- User wants parsers and serializers to be switchable by mode (server/desktop/embedded), with a default universal mode selected automatically.
- User wants lexer work (M2) to account for future web/EF connectors in design, implementing connectors one by one later.
- Parsing must support JSON strict, AJIS, and Lex modes, where Lex is permissive and JSON/AJIS are strict per their specs.

## Directive Syntax
- Directives are lines starting with `#` at line start and ending with newline/EOF; directives are lexical tokens outside strings.
- Binding rules: before root = document-level, after root = document-level trailer, otherwise bind to next value/member/element.
- Syntax: `#<namespace> <command> [key=value]...` with namespaces like AJIS/AIF/TOOL/X-; directives appear between any tokens.
- Directives to be finalized and documented together.