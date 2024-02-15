<script async setup>
import { onMounted, ref } from 'vue';
import Game from './components/Game.vue'
import speechService from './SpeechService';

const started = ref(false);

const cannotStartMic = ref(false);


async function start(id) {
  try {
    await speechService.StartMicrophone();
  }
  catch (e) {
    cannotStartMic.value = true;
  }
  await speechService.startGame(id);
  started.value = true;
}

onMounted(async () => {
  await speechService.Initialize();
  console.log(speechService.games);
});


</script>

<template>
  <v-app id="inspire">
    <!-- <v-navigation-drawer model-value class="pt-4" color="grey-lighten-3" rail>
      <v-avatar v-for="n in 6" :key="n" :color="`grey-${n === 1 ? 'darken' : 'lighten'}-1`" :size="n === 1 ? 36 : 20"
        class="d-block text-center mx-auto mb-9"></v-avatar>
    </v-navigation-drawer> -->

    <v-main>
      <v-container v-if="!speechService.gameStarted.value">
        <v-row>
          <v-col cols="12" md="6" lg="4" v-if="speechService.games.value" v-for="game in speechService.games.value">
            <v-card class="mx-auto">
              <v-card-item>
                <v-card-title>{{ game.title }}</v-card-title>
              </v-card-item>
              <v-card-text>
                <div v-if="game.minPlayers">Минимум игроков: {{ game.minPlayers }}</div>
                <div v-if="game.maxPlayers">Максимум игроков: {{ game.maxPlayers }}</div>
              </v-card-text>
              <v-divider></v-divider>
              <v-card-actions class="justify-end">
                <v-btn color="primary" variant="tonal" size="large" @click="() => start(game.id)">
                  Начать
                </v-btn>
              </v-card-actions>
            </v-card>
          </v-col>
        </v-row>
      </v-container>
      <Game v-else />
      <v-dialog v-model="cannotStartMic" width="auto">
        <v-card>
          <v-card-text>
            Не удалось активировать микрофон
          </v-card-text>
          <v-card-actions>
            <v-btn color="primary" block @click="cannotStartMic = false">Закрыть</v-btn>
          </v-card-actions>
        </v-card>
      </v-dialog>
    </v-main>
  </v-app>
</template>

<style scoped>
.start-button {
  height: 140px;
  width: 320px;
  background-color: rgb(255, 210, 126);
  will-change: filter;
  transition: filter 300ms;
  border-radius: 90px;
}

.start-button:hover {
  filter: drop-shadow(0 0 2em #646cffaa);
}
</style>
