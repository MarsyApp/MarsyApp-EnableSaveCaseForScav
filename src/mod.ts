import {DependencyContainer} from "tsyringe";
import {ILogger} from "@spt/models/spt/utils/ILogger";
import {ProfileHelper} from "@spt/helpers/ProfileHelper";
import {IPmcData} from "@spt/models/eft/common/IPmcData";
import {ItemHelper} from "@spt/helpers/ItemHelper";
import {ISaveProgressRequestData} from "@spt/models/eft/inRaid/ISaveProgressRequestData";
import {InraidCallbacks} from "@spt/callbacks/InraidCallbacks";
import {PlayerRaidEndState} from "@spt/models/enums/PlayerRaidEndState";
import {HashUtil} from "@spt/utils/HashUtil";
import * as config from "../config/config.json";

//const modConfig = require("../config/config.json");

export class Main implements IPreSptLoadMod, IPostDBLoadMod, IPostSptLoadMod {

    private modLoader: PreSptModLoader;

	preSptLoad(container: DependencyContainer): void {
		const logger = container.resolve<ILogger>("WinstonLogger");
		const itemHelper = container.resolve<ItemHelper>("ItemHelper");
		const hashUtil = container.resolve<HashUtil>("HashUtil");
		const secureContainerTemplate = config.secureContainerTemplate;
		//const secureContainerTemplate = modconfig.secureContainerTemplate;

		container.afterResolution("ProfileHelper", (_t, result: ProfileHelper) => {
			const oldRemoveSecureContainer = result.removeSecureContainer.bind(result);
			result.removeSecureContainer = (profile: IPmcData) => {
				const profileResult = oldRemoveSecureContainer(profile);
				const items = profileResult.Inventory.items;
				const defaultInventory = items.find((x) => x._tpl === "55d7217a4bdc2d86028b456d");
				const secureContainer = items.find((x) => x.slotId === "SecuredContainer");

				if (!secureContainer && defaultInventory) {
					profileResult.Inventory.items.push({
						"_id": hashUtil.generate(),
						"_tpl": secureContainerTemplate,
						"parentId": defaultInventory._id,
						"slotId": "SecuredContainer"
					});
				}

				return profileResult;
			}
		}, {frequency: "Always"});

		container.afterResolution("InraidCallbacks", (_t, result: InraidCallbacks) => {
			const oldSaveProgress = result.saveProgress.bind(result);
			result.saveProgress = (url: string, info: ISaveProgressRequestData, sessionID: string) => {

				const statusOnExit = info.exit;
				const isScav = info.isPlayerScav;
				const isDead = statusOnExit !== PlayerRaidEndState.SURVIVED && statusOnExit !== PlayerRaidEndState.RUNNER
				if (isScav && isDead) {
					const inventory = info.profile.Inventory;
					const items = inventory.items;
					const secureContainer = items.find((x) => x.slotId === "SecuredContainer");
					if (secureContainer) {
						const childItemsInSecureContainer = itemHelper.findAndReturnChildrenByItems(
							items,
							secureContainer._id
						);

						info.profile.Inventory.items = items.filter((x) => !x?.parentId || childItemsInSecureContainer.includes(x._id));
					}

					info.exit = PlayerRaidEndState.SURVIVED;
				}

				return oldSaveProgress(url, info, sessionID);
			}
		}, {frequency: "Always"});
	}
}

module.exports = { mod: new Main() }
