import * as signalR from "@microsoft/signalr";
import { MessagePackHubProtocol } from "@microsoft/signalr-protocol-msgpack";
import { reactive, ref } from "vue";
import InputContext from "./InputContext";

interface IGameInfo {
    id: string;
    title: string;
    minPlayers: number | null;
    maxPlayers: number | null;
}

class SpeechService {
    public readonly messages = reactive<
        { text: string; type: "question" | "answer" }[]
    >([]);
    //public readonly isReady = false;
    public readonly isReady = ref(false);
    private connection: signalR.HubConnection;
    public inputContext = ref<InputContext | null>(null);
    public recording = ref(false);
    private audioContext: AudioContext;
    private userMedia: MediaStream;
    private subject: signalR.Subject<Uint8Array> | null = null;
    public micEnabled = ref(true);
    public games = ref<IGameInfo[]>([]);
    public gameStarted = ref(false);
    private audios = new Set<HTMLAudioElement>();

    public async Initialize() {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/api/main")
            .configureLogging(signalR.LogLevel.Information)
            .withHubProtocol(new MessagePackHubProtocol())
            .build();

        this.connection = connection;

        connection.on("input", (id, variants, wideAnswer, timeout) => {
            this.inputContext.value = reactive<InputContext>({
                id,
                variants,
                wideAnswer,
                timeout,
                textValue: this.inputContext.value?.textValue ?? "",
            });
            if (this.userMedia) {
                this.start_microphone();
                this.subject = new signalR.Subject();
                connection.send(
                    "SendSpeechAsync",
                    this.subject,
                    id,
                    this.audioContext.sampleRate
                );
            }
            console.log("input", id, variants, wideAnswer, timeout);
        });

        connection.on("stopInput", (id, text) => {
            if (this.inputContext.value?.id === id) this.inputContext.value = null;

            if (this.subject) {
                this.subject.complete();
                this.subject = null;
            }

            if (text && text[0]) {
                this.messages.push({ text: text[0], type: "answer" });
            }
        });

        connection.on("partialInput", (id, text) => {
            if (!this.inputContext.value)
                return;
            if (this.inputContext.value.id !== id)
                return;

            this.inputContext.value.partialInput = text;
        });

        connection.on("audio", (id: string | null, text: string | null) => {
            if (text) this.messages.push({ text, type: "question" });
            if (id) this.playAudio(id);
        });

        const start = async () => {
            try {
                await connection.start();
                console.log("SignalR Connected.");
                this.isReady.value = true;
            } catch (err) {
                console.log(err);
                setTimeout(start, 5000);
            }
        };

        connection.onclose(async () => {
            await start();
        });

        // Start the connection.
        await start();

        this.games.value = await connection.invoke<IGameInfo[]>("getGames");
    }

    public async StartMicrophone() {
        if (!this.audioContext)
            this.audioContext = new AudioContext();

        if (!this.userMedia) {
            this.userMedia = await navigator.mediaDevices.getUserMedia({
                audio: true,
            });

        }
    }

    public async submitTextAnswer() {
        if (!this.inputContext.value?.textValue) return;
        console.log(
            "SendTextAsync",
            this.inputContext.value.id,
            this.inputContext.value.textValue
        );
        await this.connection.send(
            "SendTextAsync",
            this.inputContext.value.id,
            this.inputContext.value.textValue
        );
        this.inputContext.value.textValue = "";
    }

    playAudio(id) {
        //console.log('http://localhost:5108/api/audio?id=' + id);
        //return;
        var audio = new Audio("/api/audio?id=" + id);
        audio.play();

        console.log("Play audio", id);

        this.audios.add(audio);

        audio.addEventListener("ended", () => {
            this.audios.delete(audio);
            this.connection.send("ListenSpeechComplete", id);
            console.log("ListenSpeechComplete", id);
        });
    }

    async startGame(id: string) {
        await this.connection.send("StartGameAsync", id);
        this.gameStarted.value = true;
        console.log("start game", id);
        this.messages.splice(0, this.messages.length);
        this.inputContext.value = null;
    }

    async stopGame() {
        this.audios.forEach(audio => audio.pause());
        await this.connection.send("StopGame");
        this.gameStarted.value = false;
    }

    //const subject = new signalR.Subject();

    readonly bufferSize = 1024;

    start_microphone() {
        if (this.recording.value) return;
        const stream = this.userMedia;
        this.recording.value = true;
        console.log("mic started");

        var gain_node = this.audioContext.createGain();
        gain_node.connect(this.audioContext.destination);
        gain_node.gain.value = 0;

        var microphone_stream = this.audioContext.createMediaStreamSource(stream);

        var script_processor_node = this.audioContext.createScriptProcessor(
            this.bufferSize,
            1,
            1
        );
        script_processor_node.onaudioprocess =
            this.process_microphone_buffer.bind(this);

        microphone_stream.connect(script_processor_node);
        script_processor_node.connect(gain_node);
    }

    async process_microphone_buffer(event) {
        if (!this.subject) return;

        var sample = this.micEnabled.value
            ? event.inputBuffer.getChannelData(0)
            : new Float32Array(this.bufferSize);

        this.subject.next(new Uint8Array(sample.buffer));
    }
}

export const speechService = new SpeechService();
export default speechService;
