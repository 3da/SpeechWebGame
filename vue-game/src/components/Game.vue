<script setup>
import { computed, nextTick, ref, watch } from "vue";
import speechService from "../SpeechService";

defineProps({
  msg: String,
});


const inputText = ref('');

let needFocus = false;
async function submitTextAnswer(text) {
  console.log(text);
  if (speechService.inputContext.value)
    speechService.inputContext.value.textValue = text ?? inputText.value;
  await speechService.submitTextAnswer();
  //needFocus = true;
  inputText.value = '';
}

const textBox = ref(null);

const needHoldMic = ref(!!localStorage.getItem("needHoldMic"));
const holdingMic = ref(false);

if (needHoldMic.value)
  speechService.micEnabled.value = false;

watch(needHoldMic, (n) => {
  if (n) {
    speechService.micEnabled.value = false;
    localStorage.setItem("needHoldMic", n);
  }
  else
    localStorage.removeItem("needHoldMic");
});

watch(holdingMic, (mic) => {
  if (needHoldMic.value) {
    speechService.micEnabled.value = mic;
    console.log('mic enable', mic);
  }
});

watch(speechService.messages, async (newVal) => {
  await nextTick();
  window.scrollTo(0, window.scrollMaxY);
});

watch(speechService.inputContext, async (newValue, oldValue) => {
  if (!oldValue && newValue) {
    await nextTick();
    if (needFocus)
      textBox.value.focus();
    needFocus = false;
    window.scrollTo(0, window.scrollMaxY);
  }
});

</script>

<template>
  <v-container>
    <v-row no-gutters justify="center">
      <v-col cols="12" lg="8">
        <v-row v-for="(msg, index) in speechService.messages" class="mb-2 pa-0" :key="index" dense no-gutters
          :justify="msg.type == 'question' ? 'start' : 'end'">
          <v-col cols="10">
            <v-sheet v-if="msg.type == 'question'" :elevation="2" class="pa-2 pl-4 rounded-pill bg-purple-lighten-5">
              <span class="font-weight-medium">{{ msg.text }}</span>
            </v-sheet>
            <v-sheet v-else="msg.type == 'answer'" :elevation="2"
              class="pa-2 pr-4 rounded-pill bg-teal-lighten-5 text-right">
              <span class="font-weight-medium">{{ msg.text }}</span>
            </v-sheet>
          </v-col>
        </v-row>
        <v-row v-if="speechService.inputContext.value" class="mb-2 pa-0" dense no-gutters justify="end">
          <v-col cols="10">
            <v-sheet v-if="speechService.inputContext.value" :elevation="2"
              class="pa-2 pr-4 rounded-pill bg-lime-lighten-5 text-right">
              <span class="font-weight-medium">{{ speechService.inputContext.value.partialInput }} ... </span>
              <v-progress-circular indeterminate color="purple"></v-progress-circular>
            </v-sheet>
          </v-col>
        </v-row>

      </v-col>
    </v-row>
  </v-container>
  <div style="height: 120px;"></div>
  <v-sheet elevation="2" class="sticky-bottom">
    <div class="d-flex justify-space-between align-center">
      <div class="d-flex">
        <div v-if="speechService.inputContext.value?.variants">
          <v-btn variant="outlined" v-for="variant in speechService.inputContext.value?.variants"
            @click="() => submitTextAnswer(variant)" size="large" class="px-2 ma-1">{{ variant }}
          </v-btn>
        </div>
      </div>
      <div>
        <v-menu :close-on-content-click="false">
          <template v-slot:activator="{ props }">
            <v-btn icon="mdi-cog" class="ma-1" variant="outlined" color="blue" v-bind="props"></v-btn>
          </template>
          <v-list>
            <v-list-item @click="needHoldMic = !needHoldMic">
              <template v-slot:prepend>
                <v-icon icon="mdi-check" v-show="needHoldMic"></v-icon>
              </template>
              <v-list-item-title>Держать для активации микрофона</v-list-item-title>
            </v-list-item>
            <v-list-item @click="() => speechService.stopGame()">
              <template v-slot:prepend>
                <v-icon icon="mdi-close"></v-icon>
              </template>
              <v-list-item-title>Остановить игру</v-list-item-title>
            </v-list-item>
          </v-list>
        </v-menu>

        <v-btn v-if="needHoldMic" icon="mdi-microphone" class="ma-1" variant="outlined"
          :color="holdingMic ? 'green' : 'red'" @mousedown="holdingMic = true" @mouseup="holdingMic = false"
          @touchstart="holdingMic = true" @touchend="holdingMic = false" size="x-large">
        </v-btn>

        <v-btn v-else icon="mdi-microphone" class="ma-1" variant="outlined"
          :color="speechService.micEnabled.value ? 'green' : 'red'"
          @click="speechService.micEnabled.value = !speechService.micEnabled.value" size="x-large">
        </v-btn>
      </div>

    </div>
    <div class="d-flex">
      <div class="flex-grow-1">
        <v-text-field label="Ответ" hide-details v-model="inputText" ref="textBox" class="ma-1"
          @keydown.enter.prevent="() => submitTextAnswer()" variant="outlined">
        </v-text-field>
      </div>
      <v-btn variant="outlined" @click="inputText = ''" size="xlarge" class="px-2 ma-1">Очистить</v-btn>
      <v-btn variant="outlined" @click="() => submitTextAnswer()" size="xlarge" color="primary" class="px-2 ma-1"
        :disabled="!speechService.inputContext.value">Отправить</v-btn>
    </div>
  </v-sheet>


  <!-- <v-bottom-navigation :disabled="!speechService.inputContext.value">

  </v-bottom-navigation> -->
</template>

<style scoped>
.sticky-bottom {
  position: fixed;
  bottom: 0;
  width: 100%;
  padding-left: 1px;
  padding-right: 1px;
  padding-bottom: 1px;
  z-index: 100;

}
</style>
