export default interface InputContext {
    id: string;
    variants: string[] | null;
    wideAnswer: boolean;
    timeout: number;
    textValue: string;
    partialInput?: string;
}