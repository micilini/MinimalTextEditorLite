# MinimalTextEditorLite.EditorJS

Source React do editor usado pelo MinimalTextEditorLite.App.

## Instalar dependencias

```bash
npm install
```

## Rodar em modo desenvolvimento

```bash
npm start
```

## Gerar build para o app WPF

```bash
npm run build:wpf
```

Esse comando gera `build/` e copia o resultado para:

```txt
../MinimalTextEditorLite.App/EditorModules/EditorJS
```

A pasta `build/` e `node_modules/` nao devem ser commitadas.
O build copiado para `MinimalTextEditorLite.App/EditorModules/EditorJS` deve ser commitado porque o app WPF usa esses arquivos em runtime.
