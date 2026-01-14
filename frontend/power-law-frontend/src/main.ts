import { createApp } from "vue"
import App from "./App.vue"

import { createPinia } from "pinia"
import router from "./router"
import { vuetify } from "./plugins/vuetify"

createApp(App)
  .use(createPinia())
  .use(router)     // ← これが超重要
  .use(vuetify)
  .mount("#app")
