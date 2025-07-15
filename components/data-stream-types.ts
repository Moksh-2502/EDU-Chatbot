export type DataStreamDelta = {
  type: 'title' | 'id' | 'clear' | 'finish' | 'game-created' | 'game-modified';
  content: string | unknown;
};
