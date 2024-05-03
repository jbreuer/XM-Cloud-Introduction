import {
  DictionaryService,
  RestDictionaryService,
  GraphQLDictionaryService,
  constants,
} from '@sitecore-jss/sitecore-jss-nextjs';
import config from 'temp/config';

/**
 * Factory responsible for creating a DictionaryService instance
 */
export class DictionaryServiceFactory {
  /**
   * @param {string} siteName site name
   * @returns {DictionaryService} service instance
   */
  create(siteName: string): DictionaryService {
    return new GraphQLDictionaryService({
          endpoint: config.graphQLEndpoint,
          apiKey: config.sitecoreApiKey,
          siteName,
          jssAppTemplateId: '{9ED66404-64C9-4122-90E1-869CB3CEA566}',
          /*
            The Dictionary Service needs a root item ID in order to fetch dictionary phrases for the current
            app. If your Sitecore instance only has 1 JSS App, you can specify the root item ID here;
            otherwise, the service will attempt to figure out the root item for the current JSS App using GraphQL and app name.
            rootItemId: '{GUID}'
          */
        });
  }
}

/** DictionaryServiceFactory singleton */
export const dictionaryServiceFactory = new DictionaryServiceFactory();
