// Vuetify のスタイル（必須）
// これがないとコンポーネントの見た目が崩れる
import "vuetify/styles"

// Material Design Icons を使うための CSS
// v-icon で mdi-xxx が使えるようになる
import "@mdi/font/css/materialdesignicons.css"

// Vuetify 本体の createVuetify
import { createVuetify } from "vuetify"

// Vuetify が提供する全コンポーネントとディレクティブ
// autoImport を使っているが、明示的に登録しておく
import * as components from "vuetify/components"
import * as directives from "vuetify/directives"

// Vuetify インスタンスを作成
// main.ts から .use(vuetify) で読み込まれる
export const vuetify = createVuetify({
  // 使用可能な UI コンポーネント群
  components,

  // v-ripple などのディレクティブ
  directives,

  // テーマ設定
  theme: {
    // デフォルトはライトテーマ
    // 将来的に dark / カスタムテーマ切替も可能
    defaultTheme: "light",
  },
})
