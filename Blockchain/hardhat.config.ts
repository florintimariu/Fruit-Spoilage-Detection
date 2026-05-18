import { HardhatUserConfig } from "hardhat/config";
import "@nomicfoundation/hardhat-toolbox";

const config: HardhatUserConfig = {
  solidity: "0.8.28",networks: {
    sepolia: {
      type: "http",
      url: "https://eth-sepolia.g.alchemy.com/v2/w-4jO-tXn3rQeIuQnq4qV",
      accounts: ["39b40db59a6602878b8c37195739b2ec809a35ffd82b8f6b17d5aa8220741e2c"],
    }
  }
};

export default config;
