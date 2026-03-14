/// <reference types="react" />

declare module "mfeUpload/App" {
  const App: React.ComponentType;
  export default App;
}

declare module "mfeSearch/App" {
  const App: React.ComponentType;
  export default App;
}

declare module "mfeSearch/searchSlice" {
  import { Reducer } from "@reduxjs/toolkit";
  const searchReducer: Reducer;
  export default searchReducer;
}

declare module "mfeUpload/uploadSlice" {
  import { Reducer } from "@reduxjs/toolkit";
  const uploadReducer: Reducer;
  export default uploadReducer;
}
