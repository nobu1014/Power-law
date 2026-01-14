// TypeScript に「.vue ファイルは Vue コンポーネントだよ」と教えるための定義
declare module "*.vue" {
  import { DefineComponent } from "vue"
  const component: DefineComponent<{}, {}, any>
  export default component
}
